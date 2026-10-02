using System;
using System.Collections.Generic;
using UnityEngine;
using UnityTemplates.Localization;
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
        private bool _adInProgress;

        public SkinSelectionPresenter(
            ISkinSelectorView view,
            IPlayerProfileService profile,
            PlanetSkinCatalog catalog,
            IRewardedAdsService rewardedAds,
            ILocalizationService localization)
        {
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

            _view.Build(sprites, _localization.Get(LocalizationKeys.MenuSkinLocked));
            _view.SkinClicked += OnSkinClicked;
            _profile.SkinChanged += OnProfileSkinsChanged;
            _profile.SkinsUnlocked += Render;
            _localization.LocaleChanged += OnLocaleChanged;
            Render();
        }

        public void Dispose()
        {
            _view.SkinClicked -= OnSkinClicked;
            _profile.SkinChanged -= OnProfileSkinsChanged;
            _profile.SkinsUnlocked -= Render;
            _localization.LocaleChanged -= OnLocaleChanged;
        }

        private void OnLocaleChanged(LocaleChanged _) => _view.SetLockedLabel(_localization.Get(LocalizationKeys.MenuSkinLocked));

        private void OnProfileSkinsChanged(int _) => Render();

        private void Render()
        {
            for (int index = 0; index < _catalog.Count; index++)
            {
                _view.SetState(index, _profile.IsSkinUnlocked(index), index == _profile.SelectedSkin);
            }
        }

        private void OnSkinClicked(int index)
        {
            if (_profile.IsSkinUnlocked(index))
            {
                _profile.SelectSkin(index);
                return;
            }

            if (_adInProgress)
            {
                return;
            }

            _adInProgress = true;
            _rewardedAds.Show(granted =>
            {
                _adInProgress = false;
                if (!granted)
                {
                    return;
                }

                _profile.UnlockSkin(index);
                _profile.SelectSkin(index);
            });
        }
    }
}
