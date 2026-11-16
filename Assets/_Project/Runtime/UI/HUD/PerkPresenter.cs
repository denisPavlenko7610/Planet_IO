using System;
using RD.Haptics;
using RD.Localization;
using UnityEngine;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public sealed class PerkPresenter : IStartable, ITickable, IDisposable
    {
        private const float PickPitch = 1.4f;
        private const float PickVolume = 0.8f;

        private readonly IPerkView _view;
        private readonly ILocalPlayerProvider _localPlayerProvider;
        private readonly ILocalizationService _localization;
        private readonly ISfxPlayer _sfxPlayer;
        private Player _localPlayer;
        private float _offerEndTime;
        private float _offerDuration;
        private bool _offerVisible;

        public PerkPresenter(
            IPerkView view,
            ILocalPlayerProvider localPlayerProvider,
            ILocalizationService localization,
            ISfxPlayer sfxPlayer)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _localPlayerProvider = localPlayerProvider ?? throw new ArgumentNullException(nameof(localPlayerProvider));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _sfxPlayer = sfxPlayer ?? throw new ArgumentNullException(nameof(sfxPlayer));
        }

        public void Start()
        {
            _view.PerkChosen += OnPerkChosen;
            _localPlayerProvider.LocalPlayerChanged += BindPlayer;
            BindPlayer(_localPlayerProvider.LocalPlayer);
        }

        public void Dispose()
        {
            _view.PerkChosen -= OnPerkChosen;
            _localPlayerProvider.LocalPlayerChanged -= BindPlayer;
            BindPlayer(null);
        }

        public void Tick()
        {
            if (!_offerVisible)
            {
                return;
            }

            float remaining = _offerEndTime - Time.unscaledTime;
            _view.SetPerkTimer(_offerDuration > 0f ? remaining / _offerDuration : 0f);
        }

        private void BindPlayer(Player player)
        {
            if (_localPlayer != null)
            {
                _localPlayer.PerkOfferShown -= OnOfferShown;
                _localPlayer.PerkOfferClosed -= OnOfferClosed;
                _localPlayer.Defeated -= OnOfferClosed;
                _localPlayer.PerksChanged -= OnPerksChanged;
            }

            HideOffer();
            _localPlayer = player;
            if (_localPlayer == null)
            {
                _view.SetActivePerks(Array.Empty<int>(), Array.Empty<string>());
                return;
            }

            _localPlayer.PerkOfferShown += OnOfferShown;
            _localPlayer.PerkOfferClosed += OnOfferClosed;
            _localPlayer.Defeated += OnOfferClosed;
            _localPlayer.PerksChanged += OnPerksChanged;
            OnPerksChanged(_localPlayer.PerkMask);
        }

        private void OnPerksChanged(ushort mask)
        {
            var ids = new int[PerkRules.OwnedCount(mask)];
            var names = new string[ids.Length];
            int count = 0;
            for (int index = 0; index < PerkRules.Count; index++)
            {
                var id = (PerkId)index;
                if (!PerkRules.Has(mask, id))
                {
                    continue;
                }

                ids[count] = index;
                names[count] = _localization.Get(LocalizationKeys.PerkName(id));
                count++;
            }

            _view.SetActivePerks(ids, names);
        }

        private void OnOfferShown(PerkId[] perks, float seconds)
        {
            var names = new string[perks.Length];
            var descriptions = new string[perks.Length];
            for (int index = 0; index < perks.Length; index++)
            {
                names[index] = _localization.Get(LocalizationKeys.PerkName(perks[index]));
                descriptions[index] = _localization.Get(LocalizationKeys.PerkDescription(perks[index]));
            }

            _offerDuration = seconds;
            _offerEndTime = Time.unscaledTime + seconds;
            _offerVisible = true;
            _view.ShowPerkOffer(_localization.Get(LocalizationKeys.PerkTitle), names, descriptions);
        }

        private void OnOfferClosed()
        {
            HideOffer();
        }

        private void OnPerkChosen(int index)
        {
            if (!_offerVisible || _localPlayer == null)
            {
                return;
            }

            _localPlayer.ChoosePerk(index);
            _sfxPlayer.Play(SfxId.Kill, PickPitch, PickVolume);
            Haptics.Play(HapticPreset.Success);
            HideOffer();
        }

        private void HideOffer()
        {
            if (!_offerVisible)
            {
                return;
            }

            _offerVisible = false;
            _view.HidePerkOffer();
        }
    }
}
