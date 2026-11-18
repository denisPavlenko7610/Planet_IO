using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace PlanetIO.Tests
{
    public sealed class PlatformServiceIdsTests
    {

        [Test]
        public void FilledLeaderboardAndAchievementIds_AreUnique()
        {
            List<string> ids = Enum.GetValues(typeof(LeaderboardKind)).Cast<LeaderboardKind>()
                .Select(PlatformServiceIds.LeaderboardId)
                .Concat(Enum.GetValues(typeof(AchievementKind)).Cast<AchievementKind>().Select(PlatformServiceIds.AchievementId))
                .Where(id => !string.IsNullOrEmpty(id))
                .ToList();

            Assert.That(ids, Is.Unique);
            Assert.That(ids.All(id => id.Trim() == id && !id.Contains(' ')), Is.True, "IDs must not contain spaces.");
        }

        [Test]
        public void CloudSnapshotName_FitsTheGooglePlayRules()
        {
            Assert.That(PlatformServiceIds.CloudSnapshotName, Does.Match(@"^[A-Za-z0-9\-._~]{1,100}$"));
        }

        [Test]
        public void EditorIsNeverAPlayableDevice()
        {
            Assert.That(PlatformServiceIds.IsPlayableDevice, Is.False);
        }
    }
}
