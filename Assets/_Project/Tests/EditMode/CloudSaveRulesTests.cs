using NUnit.Framework;

namespace PlanetIO.Tests
{
    public sealed class CloudSaveRulesTests
    {
        private static CloudSaveData Save(
            int xp,
            int dust = 0,
            string nickname = "Nova",
            int best = 100,
            int kills = 0,
            string[] skins = null,
            string[] achievements = null,
            int lastLoginDay = 0) =>
            new()
            {
                Progression = new ProgressionState { Xp = xp, Dust = dust, LastLoginDay = lastLoginDay },
                Nickname = nickname,
                SelectedSkin = "free-1",
                UnlockedSkins = skins ?? new string[0],
                BestScore = best,
                TotalKills = kills,
                Achievements = achievements ?? new string[0]
            };

        [Test]
        public void Merge_HigherXpWinsTheProgression()
        {
            CloudMergeResult result = CloudSaveRules.Merge(Save(100, dust: 5, nickname: "Local"), Save(900, dust: 70, nickname: "Cloud"));

            Assert.That(result.Data.Progression.Xp, Is.EqualTo(900));
            Assert.That(result.Data.Progression.Dust, Is.EqualTo(70));
            Assert.That(result.Data.Nickname, Is.EqualTo("Cloud"));
            Assert.That(result.ChangesLocal, Is.True);
            Assert.That(result.ChangesCloud, Is.False);
        }

        [Test]
        public void Merge_LocalWinsWhenItIsAhead()
        {
            CloudMergeResult result = CloudSaveRules.Merge(Save(900, nickname: "Local"), Save(100, nickname: "Cloud"));

            Assert.That(result.Data.Nickname, Is.EqualTo("Local"));
            Assert.That(result.ChangesLocal, Is.False);
            Assert.That(result.ChangesCloud, Is.True);
        }

        [Test]
        public void Merge_TieOnXpPrefersTheLaterLoginAndThenLocal()
        {
            Assert.That(CloudSaveRules.Merge(Save(50, nickname: "Local", lastLoginDay: 10), Save(50, nickname: "Cloud", lastLoginDay: 11)).Data.Nickname,
                Is.EqualTo("Cloud"));
            Assert.That(CloudSaveRules.Merge(Save(50, nickname: "Local", lastLoginDay: 10), Save(50, nickname: "Cloud", lastLoginDay: 10)).Data.Nickname,
                Is.EqualTo("Local"));
        }

        [Test]
        public void Merge_TakesMaximaAndUnionsCollections()
        {
            CloudSaveData local = Save(900, best: 400, kills: 10, skins: new[] { "b", "a" }, achievements: new[] { "Rank5" });
            CloudSaveData cloud = Save(100, best: 700, kills: 30, skins: new[] { "c", "a" }, achievements: new[] { "Kills5" });

            CloudSaveData merged = CloudSaveRules.Merge(local, cloud).Data;

            Assert.That(merged.BestScore, Is.EqualTo(700));
            Assert.That(merged.TotalKills, Is.EqualTo(30));
            Assert.That(merged.UnlockedSkins, Is.EqualTo(new[] { "a", "b", "c" }));
            Assert.That(merged.Achievements, Is.EqualTo(new[] { "Kills5", "Rank5" }));
        }

		[Test]
		public void Merge_QualifiedRecordsTakeMaximaWithoutPromotingPersonalRecords()
		{
			CloudSaveData local = Save(900, best: 700, kills: 100);
			local.RankedBestScore = 300;
			local.RankedTotalKills = 10;
			CloudSaveData cloud = Save(100, best: 800, kills: 200);
			cloud.RankedBestScore = 400;
			cloud.RankedTotalKills = 5;
			CloudSaveData merged = CloudSaveRules.Merge(local, cloud).Data;
			Assert.That(merged.RankedBestScore, Is.EqualTo(400));
			Assert.That(merged.RankedTotalKills, Is.EqualTo(10));
			Assert.That(merged.BestScore, Is.EqualTo(800));
			Assert.That(merged.TotalKills, Is.EqualTo(200));
		}

		[Test]
		public void LegacySave_LeavesQualifiedRecordsAtZero()
		{
			CloudSaveData legacy = CloudSaveRules.Deserialize(System.Text.Encoding.UTF8.GetBytes(
				"{\"Version\":1,\"Progression\":{\"Xp\":0},\"Nickname\":\"Nova\",\"BestScore\":700,\"TotalKills\":100,\"UnlockedSkins\":[],\"Achievements\":[]}"));
			Assert.That(legacy.IsValid(), Is.True);
			Assert.That(legacy.RankedBestScore, Is.Zero);
			Assert.That(legacy.RankedTotalKills, Is.Zero);
		}

        [Test]
        public void Merge_IdenticalSidesChangeNothing()
        {
            CloudMergeResult result = CloudSaveRules.Merge(Save(300, skins: new[] { "x" }), Save(300, skins: new[] { "x" }));

            Assert.That(result.ChangesLocal, Is.False);
            Assert.That(result.ChangesCloud, Is.False);
        }

        [Test]
        public void Merge_MissingCloudUploadsLocal()
        {
            CloudMergeResult result = CloudSaveRules.Merge(Save(300), null);

            Assert.That(result.Data.Progression.Xp, Is.EqualTo(300));
            Assert.That(result.ChangesLocal, Is.False);
            Assert.That(result.ChangesCloud, Is.True);
        }

        [Test]
        public void Merge_InvalidCloudIsIgnored()
        {
            CloudSaveData broken = Save(9999, nickname: "");

            CloudMergeResult result = CloudSaveRules.Merge(Save(300), broken);

            Assert.That(result.Data.Progression.Xp, Is.EqualTo(300));
            Assert.That(result.ChangesCloud, Is.True);
        }

        [Test]
        public void Merge_InvalidLocalTakesTheCloud()
        {
            CloudMergeResult result = CloudSaveRules.Merge(Save(1, nickname: ""), Save(500));

            Assert.That(result.Data.Progression.Xp, Is.EqualTo(500));
            Assert.That(result.ChangesLocal, Is.True);
        }

        [Test]
        public void Merge_NothingValidReturnsNull()
        {
            Assert.That(CloudSaveRules.Merge(null, null).Data, Is.Null);
            Assert.That(CloudSaveRules.Merge(Save(1, nickname: ""), Save(2, nickname: "")).Data, Is.Null);
        }

        [Test]
        public void Serialization_RoundTripsAndRejectsGarbage()
        {
            CloudSaveData data = Save(321, dust: 9, skins: new[] { "s" }, achievements: new[] { "Rank5" });

            CloudSaveData copy = CloudSaveRules.Deserialize(CloudSaveRules.Serialize(data));

            Assert.That(copy.IsValid(), Is.True);
            Assert.That(copy.ToJson(), Is.EqualTo(data.ToJson()));
            Assert.That(CloudSaveRules.Deserialize(null), Is.Null);
            Assert.That(CloudSaveRules.Deserialize(new byte[0]), Is.Null);
            Assert.That(CloudSaveRules.Deserialize(new byte[] { 1, 2, 3 }), Is.Null);
        }
    }
}
