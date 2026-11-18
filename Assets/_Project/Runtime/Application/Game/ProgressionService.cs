using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using RD.Foundation;
using RD.Save;
using UnityEngine;

namespace PlanetIO.Application
{
    public sealed class ProgressionService : IProgressionService
    {
        private const string Slot = "player-progress";

        private readonly SaveStore _saves;
        private readonly IAnalyticsService _analytics;
        private readonly List<MissionView> _missions = new();
        private ProgressionState _state = new();
        private bool _initialized;

        public ProgressionService(SaveStore saves, IAnalyticsService analytics)
        {
            _saves = saves ?? throw new ArgumentNullException(nameof(saves));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            DayProvider = CurrentUtcDay;
        }

        public event Action Changed;

        public Func<int> DayProvider { get; set; }

        public int Xp => _state.Xp;
        public int Rank => _state.Rank;
        public float RankProgress => _state.RankProgress;
        public int Dust => _state.Dust;
        public int StreakDays => _state.StreakDays;
        public int LastStreakReward { get; private set; }
        public bool CanReroll => !_state.RerollUsed;
        public bool HasClaimableMission => _state.HasClaimableMission();
        public IReadOnlyList<MissionView> Missions => _missions;

        public async Awaitable InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_initialized)
            {
                return;
            }

            try
            {
                _state = await _saves.LoadAsync<ProgressionState>(Slot, Validate, cancellationToken);
            }
            catch (FileNotFoundException)
            {
                _state = new ProgressionState();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                UnityLogger.LogException(exception);
                _state = new ProgressionState();
            }

            _initialized = true;
            RefreshDay();
        }

        public async Awaitable FlushAsync()
        {
            if (!_initialized)
            {
                return;
            }

            await _saves.SaveAsync(Slot, _state);
        }

        public ProgressionState ExportState() => JsonUtility.FromJson<ProgressionState>(JsonUtility.ToJson(_state));

        public void ImportState(ProgressionState state)
        {
            if (!_initialized || state == null || !state.IsValid())
            {
                return;
            }

            _state = JsonUtility.FromJson<ProgressionState>(JsonUtility.ToJson(state));
            RefreshDay();
        }

        public void RefreshDay()
        {
            if (!_initialized)
            {
                return;
            }

            int reward = _state.RefreshDay(DayProvider());
            if (reward > 0)
            {
                LastStreakReward = reward;
            }

            Commit();
        }

        public RunReward ApplyRun(RunSummary summary)
        {
            if (!_initialized)
            {
                return default;
            }

            bool[] wasComplete = SnapshotCompletion();
            RunReward reward = _state.ApplyRun(summary);
            ReportCompletedMissions(wasComplete);
            Commit();
            return reward;
        }

        public bool TryClaimMission(int index)
        {
            if (!_initialized || !_state.TryClaim(index))
            {
                return false;
            }

            Commit();
            return true;
        }

        public bool TryRerollMission(int index)
        {
            if (!_initialized || !_state.TryReroll(index))
            {
                return false;
            }

            Commit();
            return true;
        }

        public bool TrySpendDust(int amount)
        {
            if (!_initialized || !_state.TrySpend(amount))
            {
                return false;
            }

            Commit();
            return true;
        }

        public void AddDust(int amount)
        {
            if (!_initialized || amount <= 0)
            {
                return;
            }

            _state.AddDust(amount);
            Commit();
        }

        private static int CurrentUtcDay() => (int)(DateTime.UtcNow - DateTime.UnixEpoch).TotalDays;

        private static void Validate(ProgressionState state)
        {
            if (state == null || !state.IsValid())
            {
                throw new InvalidSaveDataException("Unsupported or invalid Planet_IO progression.");
            }
        }

        private bool[] SnapshotCompletion()
        {
            var snapshot = new bool[_state.Missions.Length];
            for (int index = 0; index < snapshot.Length; index++)
            {
                snapshot[index] = _state.Missions[index].IsComplete;
            }

            return snapshot;
        }

        private void ReportCompletedMissions(bool[] wasComplete)
        {
            for (int index = 0; index < wasComplete.Length && index < _state.Missions.Length; index++)
            {
                MissionEntry mission = _state.Missions[index];
                if (!wasComplete[index] && mission.IsComplete)
                {
                    _analytics.Track(AnalyticsEvents.MissionComplete, ("mission_id", mission.Definition));
                }
            }
        }

        private void Commit()
        {
            RebuildViews();
            _ = SaveAsync();
            Changed?.Invoke();
        }

        private void RebuildViews()
        {
            _missions.Clear();
            foreach (MissionEntry mission in _state.Missions)
            {
                _missions.Add(new MissionView(
                    mission.Definition,
                    Mathf.Min(mission.Progress, MissionRules.Target(mission.Definition)),
                    MissionRules.Target(mission.Definition),
                    MissionRules.Reward(mission.Definition),
                    mission.IsComplete,
                    mission.Claimed));
            }
        }

        private async Awaitable SaveAsync()
        {
            try
            {
                await _saves.SaveAsync(Slot, _state);
            }
            catch (Exception exception)
            {
                UnityLogger.LogException(exception);
            }
        }
    }
}
