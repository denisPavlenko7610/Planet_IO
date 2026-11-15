using System;
using RD.Localization;
using RD.Settings;
using UnityEngine;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public sealed class FtuePresenter : IStartable, ITickable, IDisposable
    {
        private enum Step : byte
        {
            Move,
            Eat,
            Avoid,
            Hunt,
            Tier,
            Boost,
            Done
        }

        private const float StartDelaySeconds = 4.5f;
        private const float MoveDistance = 4f;
        private const float EatGain = 0.01f;
        private const float NearbyDistance = 14f;
        private const float BoostHintSeconds = 5f;
        private const float ScanIntervalSeconds = 0.3f;

        private readonly ISessionHudView _view;
        private readonly ILocalPlayerProvider _localPlayerProvider;
        private readonly ILocalizationService _localization;
        private readonly ISettingsService _settings;
        private readonly IAnalyticsService _analytics;
        private Player _player;
        private Step _step = Step.Move;
        private bool _active;
        private bool _stepShown;
        private float _stepTime;
        private float _scanTimer;
        private Vector2 _startPosition;
        private float _startCapacity;

        public FtuePresenter(
            ISessionHudView view,
            ILocalPlayerProvider localPlayerProvider,
            ILocalizationService localization,
            ISettingsService settings,
            IAnalyticsService analytics)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _localPlayerProvider = localPlayerProvider ?? throw new ArgumentNullException(nameof(localPlayerProvider));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
        }

        public void Start()
        {
            _active = !_settings.Get(GameSettingKeys.TutorialCompleted);
            _localPlayerProvider.LocalPlayerChanged += BindPlayer;
            BindPlayer(_localPlayerProvider.LocalPlayer);
        }

        public void Dispose()
        {
            _localPlayerProvider.LocalPlayerChanged -= BindPlayer;
        }

        public void Tick()
        {
            if (!_active || _player == null || !_player.IsSpawned || _player.IsDefeated)
            {
                return;
            }

            _stepTime += Time.unscaledDeltaTime;
            if (_stepTime < StartDelaySeconds && _step == Step.Move)
            {
                return;
            }

            if (!_stepShown)
            {
                ShowStep();
            }

            _scanTimer -= Time.unscaledDeltaTime;
            bool scan = _scanTimer <= 0f;
            if (scan)
            {
                _scanTimer = ScanIntervalSeconds;
            }

            EvaluateStep(scan);
        }

        private void BindPlayer(Player player)
        {
            _player = player;
            if (_player == null)
            {
                return;
            }

            _startPosition = _player.transform.position;
            _startCapacity = _player.Capacity;
        }

        private void ShowStep()
        {
            _stepShown = true;
            string key = _step switch
            {
                Step.Move => LocalizationKeys.FtueMove,
                Step.Eat => LocalizationKeys.TutorialEat,
                Step.Avoid => null,
                Step.Hunt => null,
                Step.Tier => LocalizationKeys.FtueTier,
                Step.Boost => LocalizationKeys.TutorialBoost,
                _ => null
            };

            if (key != null)
            {
                _view.ShowHint(_localization.Get(key));
            }
        }

        private void EvaluateStep(bool scan)
        {
            switch (_step)
            {
                case Step.Move:
                    if (Vector2.Distance(_player.transform.position, _startPosition) >= MoveDistance)
                    {
                        Advance(false);
                    }
                    else if (_stepTime >= StartDelaySeconds + 10f)
                    {
                        Advance(true);
                    }

                    break;

                case Step.Eat:
                    if (_player.Capacity >= _startCapacity + EatGain)
                    {
                        Advance(false);
                    }
                    else if (_stepTime >= 12f)
                    {
                        Advance(true);
                    }

                    break;

                case Step.Avoid:
                    if (scan && FindNearby(true))
                    {
                        _view.ShowHint(_localization.Get(LocalizationKeys.FtueAvoid));
                        Advance(false);
                    }
                    else if (_stepTime >= 10f)
                    {
                        Advance(true);
                    }

                    break;

                case Step.Hunt:
                    if (scan && FindNearby(false))
                    {
                        _view.ShowHint(_localization.Get(LocalizationKeys.FtueHunt));
                        Advance(false);
                    }
                    else if (_stepTime >= 10f)
                    {
                        Advance(true);
                    }

                    break;

                case Step.Tier:
                    if (_player.Tier >= 2)
                    {
                        Advance(false);
                    }
                    else if (_stepTime >= 18f)
                    {
                        Advance(true);
                    }

                    break;

                case Step.Boost:
                    if (_stepTime >= BoostHintSeconds)
                    {
                        Advance(false);
                    }

                    break;
            }
        }

        private bool FindNearby(bool dangerous)
        {
            var planets = PlanetDirectory.All;
            Vector2 position = _player.transform.position;
            for (int index = 0; index < planets.Count; index++)
            {
                PlanetScale planet = planets[index];
                if (planet == null ||
                    planet == _player ||
                    !planet.isActiveAndEnabled ||
                    !planet.IsSpawned ||
                    !planet.AreMoonsVisible ||
                    Vector2.Distance(position, planet.transform.position) > NearbyDistance)
                {
                    continue;
                }

                if (dangerous ? planet.CanDevour(_player) : _player.CanDevour(planet))
                {
                    return true;
                }
            }

            return false;
        }

        private void Advance(bool skipped)
        {
            _analytics.Track(AnalyticsEvents.TutorialStep, ("step", _step.ToString()), ("skipped", skipped ? 1 : 0));

            _step++;
            _stepShown = false;
            _stepTime = 0f;
            _startCapacity = _player.Capacity;

            if (_step != Step.Done)
            {
                return;
            }

            _active = false;
            _settings.Set(GameSettingKeys.TutorialCompleted, true);
            _settings.Flush();
        }
    }
}
