namespace PlanetIO
{
    public readonly struct RunSummary
    {
        public readonly long RunId;
        public readonly float DurationSeconds;
        public readonly int PeakScore;
        public readonly int PeakTier;
        public readonly int Kills;
        public readonly int PerksPicked;

        public RunSummary(long runId, float durationSeconds, int peakScore, int peakTier, int kills, int perksPicked)
        {
            RunId = runId;
            DurationSeconds = durationSeconds;
            PeakScore = peakScore;
            PeakTier = peakTier;
            Kills = kills;
            PerksPicked = perksPicked;
        }
    }

    public readonly struct RunReward
    {
        public readonly int XpGained;
        public readonly int DustGained;
        public readonly int XpBefore;
        public readonly int XpAfter;
        public readonly int RankBefore;
        public readonly int RankAfter;
        public readonly int MissionsCompleted;

        public RunReward(
            int xpGained,
            int dustGained,
            int xpBefore,
            int xpAfter,
            int rankBefore,
            int rankAfter,
            int missionsCompleted)
        {
            XpGained = xpGained;
            DustGained = dustGained;
            XpBefore = xpBefore;
            XpAfter = xpAfter;
            RankBefore = rankBefore;
            RankAfter = rankAfter;
            MissionsCompleted = missionsCompleted;
        }

        public bool RankedUp => RankAfter > RankBefore;
    }
}
