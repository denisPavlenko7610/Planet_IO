using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using PlanetIO.Application;
using RD.Save;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlanetIO.Tests
{
    public sealed class ProfileRecoveryTests
    {
        private const string Slot = "player-profile";
        private const string CatalogPath = "Assets/_Project/ScriptableObjects/Skins/PlanetSkins.asset";
        private const string CorruptPayload = "{ this is not json";

        [UnityTest]
        public IEnumerator LockedSkinInSave_FallsBackToFreeSkin() =>
            AwaitableTestRunner.Run(VerifyLockedSkinFallbackAsync());

        [UnityTest]
        public IEnumerator UnlockedSkinInSave_IsRestored() =>
            AwaitableTestRunner.Run(VerifyUnlockedSkinRestoredAsync());

        [UnityTest]
        public IEnumerator FailedWrite_DoesNotBlockLaterSaves() =>
            AwaitableTestRunner.Run(VerifyWriteRecoveryAsync());

        [UnityTest]
        public IEnumerator MissingSave_ImportsOnceAndPersistsSlot() =>
            AwaitableTestRunner.Run(VerifyFirstLoadPersistsAsync());

        [UnityTest]
        public IEnumerator CorruptedSave_FailsStartupAndKeepsPayload() =>
            AwaitableTestRunner.Run(VerifyCorruptedSaveAsync());

        private static async Awaitable VerifyLockedSkinFallbackAsync()
        {
            PlanetSkinCatalog catalog = LoadCatalog();
            int lockedSkin = FindSkin(catalog, free: false);
            MemoryStorage storage = new();
            SaveStore saves = new(storage, new UnityJsonSaveSerializer());
            PlayerProfileService.ProfileData data = ValidProfile();
            data.SelectedSkin = catalog.Get(lockedSkin).Id;
            await saves.SaveAsync(Slot, data);

            PlayerProfileService profile = new(catalog, saves);
            await profile.InitializeAsync();

            Assert.That(profile.SelectedSkin, Is.Not.EqualTo(lockedSkin));
            Assert.That(profile.IsSkinUnlocked(profile.SelectedSkin), Is.True);
        }

        private static async Awaitable VerifyUnlockedSkinRestoredAsync()
        {
            PlanetSkinCatalog catalog = LoadCatalog();
            int premiumSkin = FindSkin(catalog, free: false);
            string premiumId = catalog.Get(premiumSkin).Id;
            MemoryStorage storage = new();
            SaveStore saves = new(storage, new UnityJsonSaveSerializer());
            PlayerProfileService.ProfileData data = ValidProfile();
            data.SelectedSkin = premiumId;
            data.UnlockedSkins = new[] { premiumId };
            await saves.SaveAsync(Slot, data);

            PlayerProfileService profile = new(catalog, saves);
            await profile.InitializeAsync();

            Assert.That(profile.SelectedSkin, Is.EqualTo(premiumSkin));
            Assert.That(profile.IsSkinUnlocked(premiumSkin), Is.True);
        }

        private static async Awaitable VerifyWriteRecoveryAsync()
        {
            PlanetSkinCatalog catalog = LoadCatalog();
            MemoryStorage storage = new();
            SaveStore saves = new(storage, new UnityJsonSaveSerializer());
            await saves.SaveAsync(Slot, ValidProfile());
            PlayerProfileService profile = new(catalog, saves);
            await profile.InitializeAsync();

            storage.FailWrites = true;
            LogAssert.Expect(LogType.Exception, new Regex(MemoryStorage.FailureMessage));
            profile.SetNickname("Lost");
            bool failureReported = false;
            try
            {
                await profile.FlushAsync();
            }
            catch (IOException)
            {
                failureReported = true;
            }

            storage.FailWrites = false;
            profile.SetNickname("Kept");
            await profile.FlushAsync();

            PlayerProfileService reloaded = new(catalog, saves);
            await reloaded.InitializeAsync();
            Assert.That(failureReported, Is.True);
            Assert.That(reloaded.Nickname, Is.EqualTo("Kept"));
        }

        private static async Awaitable VerifyFirstLoadPersistsAsync()
        {
            PlanetSkinCatalog catalog = LoadCatalog();
            MemoryStorage storage = new();
            SaveStore saves = new(storage, new UnityJsonSaveSerializer());

            PlayerProfileService first = new(catalog, saves);
            await first.InitializeAsync();
            Assert.That(storage.Payload, Is.Not.Null);

            PlayerProfileService second = new(catalog, saves);
            await second.InitializeAsync();

            Assert.That(second.Nickname, Is.EqualTo(first.Nickname));
            Assert.That(second.BestScore, Is.EqualTo(first.BestScore));
            Assert.That(second.SelectedSkin, Is.EqualTo(first.SelectedSkin));
        }

        private static async Awaitable VerifyCorruptedSaveAsync()
        {
            MemoryStorage storage = new() { Payload = CorruptPayload };
            SaveStore saves = new(storage, new UnityJsonSaveSerializer());
            PlayerProfileService profile = new(LoadCatalog(), saves);

            Exception failure = null;
            try
            {
                await profile.InitializeAsync();
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            Assert.That(failure, Is.Not.Null);
            Assert.That(failure, Is.Not.InstanceOf<FileNotFoundException>());
            Assert.That(storage.Payload, Is.EqualTo(CorruptPayload));
        }

        private static PlanetSkinCatalog LoadCatalog()
        {
            PlanetSkinCatalog catalog = AssetDatabase.LoadAssetAtPath<PlanetSkinCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null, CatalogPath);
            return catalog;
        }

        private static int FindSkin(PlanetSkinCatalog catalog, bool free)
        {
            for (int index = 0; index < catalog.Count; index++)
            {
                if (catalog.Get(index).Free == free)
                {
                    return index;
                }
            }

            Assert.Fail($"Skin catalog has no skin with Free={free}.");
            return -1;
        }

        private static PlayerProfileService.ProfileData ValidProfile() => new()
        {
            Version = 1,
            Nickname = "Saved",
            BestScore = 12,
            SelectedSkin = string.Empty,
            UnlockedSkins = Array.Empty<string>()
        };

        private sealed class MemoryStorage : ISaveStorage
        {
            public const string FailureMessage = "disk full";

            public string Payload { get; set; }
            public bool FailWrites { get; set; }

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
                if (FailWrites)
                {
                    throw new IOException(FailureMessage);
                }

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
