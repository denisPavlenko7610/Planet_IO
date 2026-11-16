using System;
using RD.Haptics;
using RD.Localization;
using VContainer.Unity;

namespace PlanetIO.UI.Menu
{
    public sealed class ProgressMenuPresenter : IStartable, IDisposable
    {
        private readonly IProgressView _view;
        private readonly IProgressionService _progression;
        private readonly ILocalizationService _localization;
        private bool _missionsOpen;

        public ProgressMenuPresenter(
            IProgressView view,
            IProgressionService progression,
            ILocalizationService localization)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _progression = progression ?? throw new ArgumentNullException(nameof(progression));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        public void Start()
        {
            _view.MissionsRequested += OnMissionsRequested;
            _view.MissionsClosed += OnMissionsClosed;
            _view.MissionClaimRequested += OnClaimRequested;
            _view.MissionRerollRequested += OnRerollRequested;
            _progression.Changed += Render;
            _localization.LocaleChanged += OnLocaleChanged;

            _progression.RefreshDay();
            Render();
        }

        public void Dispose()
        {
            _view.MissionsRequested -= OnMissionsRequested;
            _view.MissionsClosed -= OnMissionsClosed;
            _view.MissionClaimRequested -= OnClaimRequested;
            _view.MissionRerollRequested -= OnRerollRequested;
            _progression.Changed -= Render;
            _localization.LocaleChanged -= OnLocaleChanged;
        }

        private void OnLocaleChanged(LocaleChanged _) => Render();

        private void OnMissionsRequested()
        {
            _missionsOpen = true;
            Render();
            _view.SetMissionsVisible(true);
        }

        private void OnMissionsClosed()
        {
            _missionsOpen = false;
            _view.SetMissionsVisible(false);
        }

        private void OnClaimRequested(int index)
        {
            if (_progression.TryClaimMission(index))
            {
                Haptics.Play(HapticPreset.Success);
            }
        }

        private void OnRerollRequested(int index)
        {
            _progression.TryRerollMission(index);
        }

        private void Render()
        {
            _view.ShowRankAndDust(_localization.Get(LocalizationKeys.ProgressRankDust, _progression.Rank, _progression.Dust));
            _view.SetMissionsAlert(_progression.HasClaimableMission);

            if (_missionsOpen)
            {
                RenderMissions();
            }
        }

        private void RenderMissions()
        {
            var views = _progression.Missions;
            var rows = new MissionRow[views.Count];
            string claim = _localization.Get(LocalizationKeys.MissionsDone);
            string reroll = _localization.Get(LocalizationKeys.MissionsReroll);

            for (int index = 0; index < rows.Length; index++)
            {
                MissionView mission = views[index];
                MissionRowState state = mission.Claimed
                    ? MissionRowState.Claimed
                    : mission.Complete ? MissionRowState.Claimable : MissionRowState.InProgress;
                string action = state == MissionRowState.Claimable
                    ? _localization.Get(LocalizationKeys.MissionsClaim, mission.Reward)
                    : state == MissionRowState.Claimed ? claim : $"+{mission.Reward}";

                rows[index] = new MissionRow(
                    _localization.Get(LocalizationKeys.MissionName(mission.Definition), mission.Target),
                    $"{mission.Progress}/{mission.Target}",
                    action,
                    reroll,
                    state,
                    _progression.CanReroll && !mission.Claimed && mission.Progress == 0);
            }

            string bonus = _progression.LastStreakReward > 0
                ? _localization.Get(LocalizationKeys.MissionsStreakBonus, _progression.LastStreakReward)
                : string.Empty;

            _view.ShowMissions(
                _localization.Get(LocalizationKeys.MissionsTitle),
                _localization.Get(LocalizationKeys.MissionsStreak, _progression.StreakDays),
                bonus,
                _localization.Get(LocalizationKeys.SettingsClose),
                rows);
        }
    }
}
