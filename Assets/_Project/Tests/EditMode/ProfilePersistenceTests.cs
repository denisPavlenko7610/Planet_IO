using System;
using System.Collections;
using System.IO;
using System.Threading;
using NUnit.Framework;
using PlanetIO.Application;
using RD.Save;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlanetIO.Tests
{
    public sealed class ProfilePersistenceTests
    {
        private const string Slot = "player-profile";
        private const string CatalogPath = "Assets/_Project/ScriptableObjects/Skins/PlanetSkins.asset";

        [UnityTest]
        public IEnumerator ChangesDuringSave_PersistLatestSnapshot() => Run(VerifyLatestSnapshotAsync());

        private static async Awaitable VerifyLatestSnapshotAsync()
        {
            DelayedStorage storage = new();
            SaveStore saves = new(storage, new UnityJsonSaveSerializer());
            await saves.SaveAsync(Slot, ValidProfile());
            PlanetSkinCatalog catalog = AssetDatabase.LoadAssetAtPath<PlanetSkinCatalog>(CatalogPath);
            PlayerProfileService profile = new(catalog, saves);
            await profile.InitializeAsync();
            profile.SetNickname("First");
            profile.SetNickname("Latest");
            profile.SubmitScore(321);
            await profile.FlushAsync();

            PlayerProfileService reloaded = new(catalog, saves);
            await reloaded.InitializeAsync();
            Assert.That(reloaded.Nickname, Is.EqualTo("Latest"));
            Assert.That(reloaded.BestScore, Is.EqualTo(321));
        }

        [UnityTest]
        public IEnumerator UnsupportedProfile_DoesNotOverwriteSave() => Run(VerifyInvalidProfileAsync());

        private static async Awaitable VerifyInvalidProfileAsync()
        {
            DelayedStorage storage = new();
            SaveStore saves = new(storage, new UnityJsonSaveSerializer());
            PlayerProfileService.ProfileData data = ValidProfile();
            data.Version = 999;
            await saves.SaveAsync(Slot, data);
            string original = storage.Payload;
            PlayerProfileService profile = new(AssetDatabase.LoadAssetAtPath<PlanetSkinCatalog>(CatalogPath), saves);
            bool rejected = false;
            try { await profile.InitializeAsync(); }
            catch (InvalidSaveDataException) { rejected = true; }
            Assert.That(rejected, Is.True);
            Assert.That(storage.Payload, Is.EqualTo(original));
        }

        [UnityTest]
        public IEnumerator MissingSave_ImportsLegacyOnceAndRetainsLegacyKey() => Run(VerifyLegacyImportAsync());

        private static async Awaitable VerifyLegacyImportAsync()
        {
            const string key = "PlanetIO.Nickname";
            bool existed = PlayerPrefs.HasKey(key);
            string original = PlayerPrefs.GetString(key, string.Empty);
            try
            {
                PlayerPrefs.SetString(key, "Imported");
                SaveStore saves = new(new DelayedStorage(), new UnityJsonSaveSerializer());
                PlanetSkinCatalog catalog = AssetDatabase.LoadAssetAtPath<PlanetSkinCatalog>(CatalogPath);
                PlayerProfileService profile = new(catalog, saves);
                await profile.InitializeAsync();
                Assert.That(profile.Nickname, Is.EqualTo("Imported"));
                Assert.That(PlayerPrefs.GetString(key), Is.EqualTo("Imported"));

                PlayerPrefs.SetString(key, "Obsolete");
                PlayerProfileService reloaded = new(catalog, saves);
                await reloaded.InitializeAsync();
                Assert.That(reloaded.Nickname, Is.EqualTo("Imported"));
            }
            finally
            {
                if (existed) PlayerPrefs.SetString(key, original);
                else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        private static PlayerProfileService.ProfileData ValidProfile() => new()
        {
            Version = 1,
            Nickname = "Saved",
            BestScore = 12,
            SelectedSkin = string.Empty,
            UnlockedSkins = Array.Empty<string>()
        };

        private static IEnumerator Run(Awaitable operation)
        {
            var awaiter = operation.GetAwaiter();
            while (!awaiter.IsCompleted) yield return null;
            awaiter.GetResult();
        }

        private sealed class DelayedStorage : ISaveStorage
        {
            public string Payload { get; private set; }

            public async Awaitable<bool> ExistsAsync(string slotId, CancellationToken token = default)
            {
                await System.Threading.Tasks.Task.Delay(5, token);
                return Payload != null;
            }

            public async Awaitable<string> ReadAsync(string slotId, CancellationToken token = default)
            {
                await System.Threading.Tasks.Task.Delay(5, token);
                return Payload ?? throw new FileNotFoundException();
            }

            public async Awaitable WriteAsync(string slotId, string payload, CancellationToken token = default)
            {
                await System.Threading.Tasks.Task.Delay(5, token);
                Payload = payload;
            }

            public async Awaitable DeleteAsync(string slotId, CancellationToken token = default)
            {
                await System.Threading.Tasks.Task.Delay(5, token);
                Payload = null;
            }
        }
    }
}
