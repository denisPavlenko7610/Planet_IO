using System.Collections;
using System.Linq;
using NUnit.Framework;
using PlanetIO.Application;
using PlanetIO.Infrastructure.Networking;
using System.Reflection;
using PlanetIO.Bootstrap;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;
using Object = UnityEngine.Object;

namespace PlanetIO.Tests
{
    public sealed partial class GameFlowTests
    {
        [UnityTest, Timeout(90000)]
        public IEnumerator Migration_RestartsNgoAndRestoresWorldWithoutStartingANewRun()
        {
            yield return StartMoonGame();

            Player player = PrepareMoonPlayer();
            player.Capacity = 0.9f;
            yield return null;

            player.ChoosePerkRpc(0);
            yield return null;

            var game = Object.FindFirstObjectByType<GameLifetimeScope>();
            game.Container.Resolve<ILootSpawnService>().SpawnLoot(new Vector2(70, 70), 1f);
			FoodSpawner foodSpawner = Object.FindFirstObjectByType<FoodSpawner>();
			FoodRecord protectedDrop = foodSpawner.SpawnAt(player.transform, 0.07f);
			protectedDrop.SetDropper(player.NetworkObjectId, 5f);
			ulong dropId = protectedDrop.Id;
            var store = Resolve<MatchMigrationStore>();
            store.BeginRecovery();
            long runId = game.Container.Resolve<IRunStats>().Current.RunId;
            byte[] data = store.Capture();
            Assert.That(data.Length, Is.LessThan(MatchSnapshotCodec.MaximumCompressedBytes));
            var snapshot = MatchSnapshotCodec.Decode(data);
            var expected = snapshot.Players.Single(p => p.Identity == "solo");
            expected.Durability[0] = 1;
            expected.Durability[1] = 0;
            expected.Regrow[1] = 4f;
            data = MatchSnapshotCodec.Encode(snapshot);
            int foodCount = snapshot.Food.Count(f => !f.Inactive);
            float nutrition = snapshot.Food.Where(f => f.Dropped).Sum(f => f.Nutrition);
            var manager = NetworkManager.Singleton;
            var sessionService = Resolve<NetworkSessionService>();
            var shutdownFlag = typeof(NetworkSessionService).GetField(
                "_shutdownRequested",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            shutdownFlag.SetValue(sessionService, true);
            manager.Shutdown();
            while (manager.ShutdownInProgress || manager.IsListening)
            {
                yield return null;
            }

            shutdownFlag.SetValue(sessionService, false);
            store.Accept(data);
            Assert.That(manager.StartHost(), Is.True);
            var result = manager.SceneManager.LoadScene(SceneNames.Game, LoadSceneMode.Single);
            Assert.That(result, Is.EqualTo(SceneEventProgressStatus.Started));
            yield return WaitUntil(() => !store.HasPendingWorld && FindLocalPlayer() != null, 30f);

            player = FindLocalPlayer();
            Assert.That(player.WasMigrationRestored, Is.True);
			FoodRecord restoredDrop = Object.FindFirstObjectByType<FoodSpawner>().World.Find(dropId);
			Assert.That(restoredDrop, Is.Not.Null);
			Assert.That(restoredDrop.IsMagnetImmuneFor(player.NetworkObjectId), Is.True);
			Assert.That(restoredDrop.ExpiresAt, Is.GreaterThan(0f));
			Assert.That(AllFood().Count(food => !food.IsDropped), Is.EqualTo(2000));

            Assert.That(player.Capacity, Is.EqualTo(expected.Mass).Within(0.00001f));
            Assert.That(player.PerkMask, Is.EqualTo(expected.Perks));
            Assert.That(player.CaptureMigration().Durability[0], Is.EqualTo(1));
            Assert.That(player.CaptureMigration().Regrow[1], Is.EqualTo(4f).Within(0.05f));
            Assert.That(
                AllFood().Count(f => f.Active),
                Is.EqualTo(foodCount)
            );
            Assert.That(SumDroppedNutrition(), Is.EqualTo(nutrition).Within(0.0001f));
            game = Object.FindFirstObjectByType<GameLifetimeScope>();
            yield return null;

            Assert.That(game.Container.Resolve<IRunStats>().Current.RunId, Is.EqualTo(runId));
            Assert.That(OrbitClock.Now, Is.GreaterThanOrEqualTo(snapshot.Clock));
            store.CompleteRecovery();
            yield return WaitForGameplay(5f);
        }
    }
}
