using System;

namespace PlanetIO
{
    public interface IBlitzMatch
    {
        event Action Finished;

        bool IsEnabled { get; }
        bool IsFinished { get; }
        int FinalRank { get; }
        int FinalCount { get; }

        void Restart();
    }
}
