#if UNITY_EDITOR || PLANET_IO_MIGRATION_PROBE
using System;
using System.IO;
using System.Linq;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using VContainer;
using Object = UnityEngine.Object;

namespace PlanetIO.Bootstrap
{
    public static class HostMigrationPeerProbe
    {
        private static string Arg(string key)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        public static string StoragePath(string fallback)
        {
            string directory = Arg("--migration-probe-dir"), role = Arg("--migration-probe-role");
            return directory != null && role != null ? Path.Combine(directory, "saves-" + role) : fallback;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Start()
        {
            if (Arg("--migration-probe-role") != null)
            {
                _ = Run();
            }
        }

        private static async Awaitable Wait(Func<bool> predicate, float seconds = 70f)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!predicate())
            {
                if (Time.realtimeSinceStartup > end)
                {
                    throw new TimeoutException("Peer probe condition timed out.");
                }

                await Awaitable.NextFrameAsync();
            }
        }

        private static Player Local() => NetworkManager.Singleton?.LocalClient?.PlayerObject?.GetComponent<Player>();
        private static GameLifetimeScope Game() => Object.FindFirstObjectByType<GameLifetimeScope>();

        private static async Awaitable Run()
        {
            string role = Arg("--migration-probe-role"), directory = Arg("--migration-probe-dir");
            Directory.CreateDirectory(directory);
            try
            {
                UnityEngine.Application.runInBackground = true;
                File.WriteAllText(Path.Combine(directory, role + ".stage"), "boot");
                await Wait(() => Object.FindFirstObjectByType<ApplicationLifetimeScope>()?.Container != null);
                var content = Addressables.InitializeAsync(false);
                content.WaitForCompletion();
                Addressables.Release(content);
                if (SceneManager.GetActiveScene().name != SceneNames.Menu)
                {
                    SceneManager.LoadScene(SceneNames.Menu);
                }
                await Awaitable.NextFrameAsync();
                File.WriteAllText(Path.Combine(directory, role + ".stage"), "authentication");
                await UnityServices.InitializeAsync();
                if (AuthenticationService.Instance.IsSignedIn)
                {
                    AuthenticationService.Instance.SignOut();
                }
                AuthenticationService.Instance.SwitchProfile("migration-probe-" + role);
                var app = Object.FindFirstObjectByType<ApplicationLifetimeScope>();
                var session = app.Container.Resolve<INetworkSessionService>();
                var store = app.Container.Resolve<MatchMigrationStore>();
                if (role == "host")
                {
                    File.WriteAllText(Path.Combine(directory, role + ".stage"), "creating room");
                    if (!await session.StartHostAsync(4))
                    {
                        throw new Exception(session.Status);
                    }

                    File.WriteAllText(Path.Combine(directory, "room.txt"), session.CurrentRoom.RoomCode);
                    await Wait(() =>
                        NetworkManager.Singleton.ConnectedClients.Count == 3 &&
                        Game()?.Container != null &&
                        Game().Container.Resolve<IGameStateService>().IsGameplayActive
                    );
                    foreach (Player player in Object.FindObjectsByType<Player>(FindObjectsInactive.Exclude))
                    {
                        player.Capacity = 1.2f;
                        player.GetComponent<Collider2D>().enabled = false;
                    }
                    foreach (Enemy bot in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude))
                    {
                        bot.GetComponent<Collider2D>().enabled = false;
                    }
                    await Awaitable.WaitForSecondsAsync(1f);
                    store.BeginRecovery();
                    File.WriteAllBytes(Path.Combine(directory, "expected.bin"), store.Capture());
                    await Wait(() =>
                        File.Exists(Path.Combine(directory, "client1.ready")) &&
                        File.Exists(Path.Combine(directory, "client2.ready"))
                    );
                    if (Arg("--migration-probe-mode") == "abrupt")
                    {
                        File.WriteAllText(Path.Combine(directory, "host.awaiting-kill"), "ready");
                        await Wait(() => false, 150f);
                    }
                    await session.ShutdownAndReturnToMenuAsync();
                    File.WriteAllText(Path.Combine(directory, "host.left"), "graceful");
                }
                else
                {
                    await Wait(() => File.Exists(Path.Combine(directory, "room.txt")));
                    File.WriteAllText(Path.Combine(directory, role + ".stage"), "joining room");
                    if (!await session.StartClientAsync(File.ReadAllText(Path.Combine(directory, "room.txt"))))
                    {
                        throw new Exception(session.Status);
                    }

                    await Wait(() =>
                        Local() != null &&
                        Game()?.Container != null &&
                        Game().Container.Resolve<IGameStateService>().IsGameplayActive &&
                        File.Exists(Path.Combine(directory, "expected.bin"))
                    );
                    var expected =
                        MatchSnapshotCodec.Decode(File.ReadAllBytes(Path.Combine(directory, "expected.bin")));
                    var own = expected.Players.Single(p => p.Identity == AuthenticationService.Instance.PlayerId);
                    long runId = Game().Container.Resolve<IRunStats>().Current.RunId;
                    Player previous = Local();
                    File.WriteAllText(Path.Combine(directory, role + ".ready"), "ready");
                    for (int epoch = 1; epoch <= 2; epoch++)
                    {
                        File.WriteAllText(Path.Combine(directory, role + ".stage"), "migration " + epoch);
                        await Wait(() =>
                            Local() != null &&
                            !ReferenceEquals(Local(), previous) &&
                            Local().WasMigrationRestored &&
                            !store.IsRecovering &&
                            session.State == NetworkSessionState.InGame
                        );
                        if (Mathf.Abs(Local().Capacity - own.Mass) > 0.03f)
                        {
                            throw new Exception("Restored player mass differs from the final snapshot.");
                        }
                        if (Local().PerkMask != own.Perks)
                        {
                            throw new Exception("Restored perks differ.");
                        }
                        if (Game().Container.Resolve<IRunStats>().Current.RunId != runId)
                        {
                            throw new Exception("Migration started a new run.");
                        }

                        File.WriteAllText(
                            Path.Combine(directory, role + ".pass" + epoch),
                            "host=" +
                            NetworkManager.Singleton.IsServer +
                            ";mass=" +
                            Local().Capacity +
                            ";runPreserved=true"
                        );
                        previous = Local();
                        if (epoch == 1 && NetworkManager.Singleton.IsServer)
                        {
                            await Wait(() =>
                                File.Exists(Path.Combine(directory, "client1.pass1")) &&
                                File.Exists(Path.Combine(directory, "client2.pass1"))
                            );
                            await session.ShutdownAndReturnToMenuAsync();
                            File.WriteAllText(Path.Combine(directory, role + ".left"), "graceful");
                            break;
                        }
                        if (epoch == 2)
                        {
                            await session.ShutdownAndReturnToMenuAsync();
                        }
                    }
                }
                UnityEngine.Application.Quit(0);
            }
            catch (Exception error)
            {
                File.WriteAllText(Path.Combine(directory, role + ".failure"), error.ToString());
                Debug.LogException(error);
                UnityEngine.Application.Quit(2);
            }
        }
    }
}
#endif
