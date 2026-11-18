using System.Text;
using NUnit.Framework;

namespace PlanetIO.Tests
{
	public sealed class CloudSaveInputTests
	{
		private static CloudSaveData Local() =>
			new()
			{
				Progression = new ProgressionState { Xp = 300 },
				Nickname = "Local",
				SelectedSkin = "free-1",
				BestScore = 120
			};

		[TestCase("{}")]
		[TestCase("null")]
		[TestCase("[]")]
		[TestCase("\"text\"")]
		[TestCase("{\"Version\":99}")]
		[TestCase("{\"Nickname\":\"x\",\"BestScore\":-5}")]
		[TestCase("{\"Nickname\":\"x\",\"TotalKills\":-1}")]
		[TestCase("{\"Version\":1,\"Nickname\":\"x\",\"Progression\":{\"Version\":1,\"Xp\":-10}}")]
		public void Deserialize_NeverThrowsAndNeverYieldsUsableData(string json)
		{
			CloudSaveData data = null;

			Assert.DoesNotThrow(() => data = CloudSaveRules.Deserialize(Encoding.UTF8.GetBytes(json)));
			Assert.That(data == null || !data.IsValid(), Is.True, json);
		}

		[TestCase("{}")]
		[TestCase("[]")]
		[TestCase("{\"Version\":99}")]
		[TestCase("{\"Nickname\":\"x\",\"BestScore\":-5}")]
		public void Merge_GarbageCloudKeepsTheLocalProgress(string json)
		{
			CloudSaveData cloud = CloudSaveRules.Deserialize(Encoding.UTF8.GetBytes(json));

			CloudMergeResult result = CloudSaveRules.Merge(Local(), cloud);

			Assert.That(result.Data, Is.Not.Null);
			Assert.That(result.Data.Progression.Xp, Is.EqualTo(300));
			Assert.That(result.Data.Nickname, Is.EqualTo("Local"));
			Assert.That(result.ChangesLocal, Is.False);
		}

		[Test]
		public void Merge_DoesNotMutateItsInputs()
		{
			CloudSaveData local = Local();
			local.UnlockedSkins = new[] { "b", "a" };
			CloudSaveData cloud = Local();
			cloud.Progression.Xp = 900;
			cloud.UnlockedSkins = new[] { "c" };
			string localBefore = local.ToJson();
			string cloudBefore = cloud.ToJson();

			CloudSaveRules.Merge(local, cloud);

			Assert.That(local.ToJson(), Is.EqualTo(localBefore));
			Assert.That(cloud.ToJson(), Is.EqualTo(cloudBefore));
		}

		[Test]
		public void Merge_IsStableWhenAppliedTwice()
		{
			CloudSaveData local = Local();
			local.UnlockedSkins = new[] { "b", "a" };
			CloudSaveData cloud = Local();
			cloud.Progression.Xp = 900;
			cloud.UnlockedSkins = new[] { "c", "a" };
			cloud.BestScore = 500;
			cloud.TotalKills = 12;

			CloudSaveData once = CloudSaveRules.Merge(local, cloud).Data;
			CloudMergeResult twice = CloudSaveRules.Merge(once, cloud);

			Assert.That(twice.Data.ToJson(), Is.EqualTo(once.ToJson()));
			Assert.That(twice.ChangesLocal, Is.False);
		}
	}
}
