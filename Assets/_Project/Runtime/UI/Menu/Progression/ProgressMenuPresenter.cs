using System;
using System.Collections.Generic;
using RD.Haptics;
using RD.Localization;
using VContainer.Unity;

namespace PlanetIO.UI.Menu
{
    public sealed class ProgressMenuPresenter : IStartable, ITickable, IDisposable
    {
        private readonly IProgressView _view;
        private readonly IProgressionService _progression;
        private readonly ILocalizationService _localization;
		private readonly IRewardedAdsService _rewardedAds;
		private bool _rerollPending;
		private bool _rerollAdUnavailable;
		private bool _disposed;
		private bool _missionsOpen;
		private DateTime _nextResetUtc;

        public ProgressMenuPresenter(
            IProgressView view,
			IProgressionService progression,
			ILocalizationService localization,
			IRewardedAdsService rewardedAds)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _progression = progression ?? throw new ArgumentNullException(nameof(progression));
			_localization = localization ?? throw new ArgumentNullException(nameof(localization));
			_rewardedAds = rewardedAds ?? throw new ArgumentNullException(nameof(rewardedAds));
        }

        public void Start()
        {
            _view.MissionsRequested += OnMissionsRequested;
            _view.MissionsClosed += OnMissionsClosed;
            _view.MissionClaimRequested += OnClaimRequested;
            _view.MissionRerollRequested += OnRerollRequested;
            _progression.Changed += Render;
            _localization.LocaleChanged += OnLocaleChanged;

			RefreshDay(DateTime.UtcNow);
        }

        public void Dispose()
        {
			_disposed = true;
			_view.MissionsRequested -= OnMissionsRequested;
            _view.MissionsClosed -= OnMissionsClosed;
            _view.MissionClaimRequested -= OnClaimRequested;
            _view.MissionRerollRequested -= OnRerollRequested;
            _progression.Changed -= Render;
            _localization.LocaleChanged -= OnLocaleChanged;
        }

		public void Tick()
		{
			DateTime now = DateTime.UtcNow;
			if (now >= _nextResetUtc)
			{
				RefreshDay(now);
			}
		}

		private void RefreshDay(DateTime now)
		{
			_nextResetUtc = now.Date.AddDays(1);
			_rerollAdUnavailable = false;
			_progression.RefreshDay();
			Render();
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
			DateTime now = DateTime.UtcNow;
			if (now >= _nextResetUtc)
			{
				RefreshDay(now);
				return;
			}

			IReadOnlyList<MissionView> missions = _progression.Missions;
			if (_rerollPending || index < 0 || index >= missions.Count || !_progression.CanReroll ||
				missions[index].Claimed || missions[index].Progress != 0)
			{
				return;
			}

			DateTime day = now.Date;
			int definition = missions[index].Definition;
			_rerollPending = true;
			_rerollAdUnavailable = false;
			Render();

			_rewardedAds.Show(granted =>
			{
				if (_disposed)
				{
					return;
				}

				_rerollPending = false;
				DateTime completedAt = DateTime.UtcNow;
				if (completedAt >= _nextResetUtc)
				{
					RefreshDay(completedAt);
				}

				_rerollAdUnavailable = !granted;
				IReadOnlyList<MissionView> current = _progression.Missions;
				if (granted && completedAt.Date == day && index < current.Count &&
					current[index].Definition == definition)
				{
					_progression.TryRerollMission(index);
				}

				Render();
			});
		}

        private void Render()
        {
			IReadOnlyList<MissionView> missions = _progression.Missions;
			int claimableCount = 0;
			foreach (MissionView mission in missions)
			{
				if (mission.Complete && !mission.Claimed)
				{
					claimableCount++;
				}
			}

			_view.ShowMissionsEntry(_localization.Get(LocalizationKeys.MissionsButton), claimableCount);

            if (_missionsOpen)
            {
                RenderMissions();
            }
        }

        private void RenderMissions()
        {
			IReadOnlyList<MissionView> views = _progression.Missions;
			MissionRow[] rows = new MissionRow[views.Count];
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
					: state == MissionRowState.Claimed ? claim : string.Empty;

                rows[index] = new MissionRow(
                    _localization.Get(LocalizationKeys.MissionName(mission.Definition), mission.Target),
					_localization.Get(LocalizationKeys.MissionProgressReward, Math.Min(mission.Progress, mission.Target),
						mission.Target, mission.Reward),
                    action,
                    reroll,
                    state,
					!_rerollPending && _progression.CanReroll && !mission.Claimed && mission.Progress == 0);
            }

            string bonus = _progression.LastStreakReward > 0
                ? _localization.Get(LocalizationKeys.MissionsStreakBonus, _progression.LastStreakReward)
                : string.Empty;

			_view.ShowMissionRerollStatus(_rerollAdUnavailable
				? _localization.Get(LocalizationKeys.MenuAdUnavailable)
				: string.Empty);
			_view.ShowMissionsReset(_localization.Get(LocalizationKeys.MissionsReset, _nextResetUtc.ToLocalTime().ToString("t")));

            _view.ShowMissions(
                _localization.Get(LocalizationKeys.MissionsTitle),
                _localization.Get(LocalizationKeys.MissionsStreak, _progression.StreakDays),
                bonus,
                _localization.Get(LocalizationKeys.SettingsClose),
                rows);
        }
    }
}
