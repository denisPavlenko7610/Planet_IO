using System;
using System.Collections.Generic;
using UnityEngine;
using RD.Localization;
using VContainer.Unity;

namespace PlanetIO.UI.Menu
{
	public sealed class SkinSelectionPresenter : IStartable, IDisposable
	{
		private readonly ISkinSelectorView _view;
		private readonly IPlayerProfileService _profile;
		private readonly PlanetSkinCatalog _catalog;
		private readonly IRewardedAdsService _rewardedAds;
		private readonly ILocalizationService _localization;
		private readonly IProgressionService _progression;
		private readonly IAnalyticsService _analytics;
		private bool _adInProgress;
		private bool _disposed;

		public SkinSelectionPresenter(
			ISkinSelectorView view,
			IPlayerProfileService profile,
			PlanetSkinCatalog catalog,
			IRewardedAdsService rewardedAds,
			ILocalizationService localization,
			IProgressionService progression,
			IAnalyticsService analytics)
		{
			_progression = progression ?? throw new ArgumentNullException(nameof(progression));
			_analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
			_view = view ?? throw new ArgumentNullException(nameof(view));
			_profile = profile ?? throw new ArgumentNullException(nameof(profile));
			_catalog = catalog ? catalog : throw new ArgumentNullException(nameof(catalog));
			_rewardedAds = rewardedAds ?? throw new ArgumentNullException(nameof(rewardedAds));
			_localization = localization ?? throw new ArgumentNullException(nameof(localization));
		}

		public void Start()
		{
			List<Sprite> sprites = new(_catalog.Count);
			for (int index = 0; index < _catalog.Count; index++)
			{
				sprites.Add(_catalog.Get(index).Sprite);
			}

			_view.Build(sprites, _profile.SelectedSkin);
			RenderLabels();
			_view.SkinClicked += OnSkinClicked;
			_view.RewardedUnlockRequested += OnRewardedUnlockRequested;
			_progression.Changed += Render;
			_profile.SkinChanged += OnProfileSkinsChanged;
			_profile.SkinsUnlocked += Render;
			_localization.LocaleChanged += OnLocaleChanged;
			Render();
		}

		public void Dispose()
		{
			_disposed = true;
			_view.SkinClicked -= OnSkinClicked;
			_view.RewardedUnlockRequested -= OnRewardedUnlockRequested;
			_progression.Changed -= Render;
			_profile.SkinChanged -= OnProfileSkinsChanged;
			_profile.SkinsUnlocked -= Render;
			_localization.LocaleChanged -= OnLocaleChanged;
		}

		private void OnLocaleChanged(LocaleChanged _) => RenderLabels();

		private void RenderLabels()
		{
			string[] names = new string[_catalog.Count];
			for (int index = 0; index < names.Length; index++)
			{
				names[index] = _localization.Get(LocalizationKeys.SkinName(_catalog.Get(index).Id));
			}

			_view.SetLabels(names,
				_localization.Get(LocalizationKeys.MenuSkinSelect),
				_localization.Get(LocalizationKeys.MenuSkinEquipped),
				_localization.Get(LocalizationKeys.MenuSkinBuy, DustRules.SkinPrice),
				_localization.Get(LocalizationKeys.MenuSkinWatch));
		}

		private void OnProfileSkinsChanged(int _) => Render();

		private void Render()
		{
			for (int index = 0; index < _catalog.Count; index++)
			{
				_view.SetState(index, _profile.IsSkinUnlocked(index), index == _profile.SelectedSkin);
			}

			_view.SetActions(_progression.Dust >= DustRules.SkinPrice, _adInProgress);
		}

		private void OnSkinClicked(int index)
		{
			if (_adInProgress)
			{
				return;
			}

			_view.ShowAdStatus(string.Empty, false);
			if (_profile.IsSkinUnlocked(index))
			{
				_profile.SelectSkin(index);
				return;
			}

			if (!_progression.TrySpendDust(DustRules.SkinPrice))
			{
				return;
			}

			_profile.UnlockSkin(index);
			_profile.SelectSkin(index);
			_analytics.Track(
				AnalyticsEvents.ShopPurchase,
				("item_id", _catalog.Get(index).Id),
				("currency", "dust"));
		}

		private void OnRewardedUnlockRequested(int index)
		{
			if (_adInProgress || _profile.IsSkinUnlocked(index))
			{
				return;
			}

			_adInProgress = true;
			Render();
			_view.ShowAdStatus(_localization.Get(LocalizationKeys.StatusLoading), false);
			_rewardedAds.Show(granted =>
			{
				if (_disposed || !_adInProgress)
				{
					return;
				}

				_adInProgress = false;
				if (granted)
				{
					_profile.UnlockSkin(index);
					_profile.SelectSkin(index);
				}

				Render();
				_view.ShowAdStatus(granted ? string.Empty : _localization.Get(LocalizationKeys.MenuAdUnavailable), !granted);
			});
		}
	}
}
