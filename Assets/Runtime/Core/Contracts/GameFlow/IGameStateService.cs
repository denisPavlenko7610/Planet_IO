using System;

namespace Planet_IO
{
    public interface IGameStateService
    {
        event Action<GameState, GameState> StateChanged;

        GameState State { get; }
        bool IsGameplayActive { get; }

        void FinishGame();
        void BeginShutdown();
    }
}
