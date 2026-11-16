using System;
using RD.Localization;
using UnityEngine;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public sealed class EventBannerPresenter : IStartable, ITickable, IDisposable
    {
        private const float ModifierDelaySeconds = 8f;

        private readonly IWorldHudView _view;
        private readonly IWorldEvents _events;
        private readonly ILocalizationService _localization;
        private readonly IGameStateService _gameState;
        private readonly ISfxPlayer _sfxPlayer;
        private float _playing;
        private bool _modifierShown;

        public EventBannerPresenter(
            IWorldHudView view,
            IWorldEvents events,
            ILocalizationService localization,
            IGameStateService gameState,
            ISfxPlayer sfxPlayer)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
            _sfxPlayer = sfxPlayer ?? throw new ArgumentNullException(nameof(sfxPlayer));
        }

        public void Start()
        {
            _events.Warned += OnWarned;
            _events.Started += OnStarted;
        }

        public void Dispose()
        {
            _events.Warned -= OnWarned;
            _events.Started -= OnStarted;
        }

        public void Tick()
        {
            if (_modifierShown || !_gameState.IsGameplayActive)
            {
                return;
            }

            _playing += Time.unscaledDeltaTime;
            if (_playing < ModifierDelaySeconds)
            {
                return;
            }

            _modifierShown = true;
            string key = LocalizationKeys.ModifierName(_events.Modifier);
            if (key != null)
            {
                _view.ShowEventBanner(_localization.Get(key));
            }
        }

        private void OnWarned(CosmicEventType type)
        {
            _view.ShowEventBanner(_localization.Get(LocalizationKeys.EventWarning(type)));
            _sfxPlayer.Play(SfxId.EventWarning);
        }

        private void OnStarted(CosmicEventType type)
        {
            _view.ShowEventBanner(_localization.Get(LocalizationKeys.EventStarted(type)));
        }
    }
}
