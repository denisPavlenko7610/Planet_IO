using System;
using System.Collections.Generic;
using RD.Localization;
using RD.Pause;
using RD.Settings;
using RD.Tutorials;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
	public sealed class FtuePresenter : IStartable, ITickable, IDisposable
	{
		private enum Lesson
		{
			Move,
			Food,
			Bot,
			Comet,
			BlackHole,
			Evolution,
			Boost,
			Rank,
			Count
		}

		private const float ScanInterval = 0.2f;
		private const float FoodNoticeDistance = 4f;
		private const float CometNoticeDistance = 5f;
		private const float BotNoticeDistance = 6f;

		private readonly SessionHudView _hud;
		private readonly ILocalPlayerProvider _players;
		private readonly ILocalizationService _localization;
		private readonly ISettingsService _settings;
		private readonly IAnalyticsService _analytics;
		private readonly INetworkSessionService _session;
		private readonly IGameStateService _game;
		private readonly IPauseService _pauses;
		private readonly IFoodService _food;
		private readonly IProgressionService _progression;
		private readonly TutorialService _tutorials = new(new PlayerPrefsTutorialProgressStore("PlanetIO.Onboarding."));
		private readonly TutorialDefinition[] _lessons = new TutorialDefinition[(int)Lesson.Count];
		private readonly List<FoodRecord> _nearbyFood = new();
		private IDisposable _pause;
		private int _lessonsRemaining;
		private int _lastRank;
		private bool _rankIncreased;
		private float _nextScan;

		public FtuePresenter(
			ISessionHudView view,
			ILocalPlayerProvider players,
			ILocalizationService localization,
			ISettingsService settings,
			IAnalyticsService analytics,
			INetworkSessionService session,
			IGameStateService game,
			IPauseService pauses,
			IFoodService food,
			IProgressionService progression)
		{
			_hud = (SessionHudView)view;
			_players = players;
			_localization = localization;
			_settings = settings;
			_analytics = analytics;
			_session = session;
			_game = game;
			_pauses = pauses;
			_food = food;
			_progression = progression;
		}

		public void Start()
		{
			if (_session.Mode != NetworkSessionMode.SinglePlayer || _settings.Get(GameSettingKeys.TutorialCompleted))
			{
				return;
			}

			AddLesson(Lesson.Move, "move", LocalizationKeys.FtueMove);
			AddLesson(Lesson.Food, "food", LocalizationKeys.TutorialEat);
			AddLesson(Lesson.Bot, "bot", LocalizationKeys.TutorialBot);
			AddLesson(Lesson.Comet, "comet", LocalizationKeys.TutorialComet);
			AddLesson(Lesson.BlackHole, "black-hole", LocalizationKeys.TutorialBlackHole);
			AddLesson(Lesson.Evolution, "evolution", LocalizationKeys.TutorialEvolution);
			AddLesson(Lesson.Boost, "boost", LocalizationKeys.TutorialBoost);
			AddLesson(Lesson.Rank, "rank", LocalizationKeys.TutorialRank);
			_lastRank = _progression.Rank;

			_tutorials.Started += OnLessonStarted;
			_tutorials.StepChanged += RenderLesson;
			_tutorials.Ended += OnLessonEnded;
			_progression.Changed += OnProgressionChanged;
			_localization.LocaleChanged += OnLocaleChanged;
			_hud.TutorialContinued += OnContinue;
			_hud.TutorialSkipped += OnSkip;
		}

		public void Dispose()
		{
			_tutorials.Cancel();
			_tutorials.Started -= OnLessonStarted;
			_tutorials.StepChanged -= RenderLesson;
			_tutorials.Ended -= OnLessonEnded;
			_progression.Changed -= OnProgressionChanged;
			_localization.LocaleChanged -= OnLocaleChanged;
			_hud.TutorialContinued -= OnContinue;
			_hud.TutorialSkipped -= OnSkip;

			foreach (TutorialDefinition lesson in _lessons)
			{
				if (lesson != null)
				{
					UnityEngine.Object.Destroy(lesson);
				}
			}
		}

		public void Tick()
		{
			if (_lessonsRemaining == 0 || _pauses.IsPaused || _session.Mode != NetworkSessionMode.SinglePlayer)
			{
				return;
			}

			Player player = _players.LocalPlayer;
			if (player == null || !player.IsSpawned)
			{
				return;
			}

			if (player.IsDefeated)
			{
				if (_rankIncreased && _hud.IsShowingDefeat)
				{
					TryShow(Lesson.Rank);
				}

				return;
			}

			if (!_game.IsGameplayActive || !_hud.CanShowTutorial)
			{
				return;
			}

			if (TryShow(Lesson.Move) ||
				(player.Tier > TierRules.MinTier && TryShow(Lesson.Evolution)) ||
				(player.Capacity >= 0.12f && TryShow(Lesson.Boost)))
			{
				return;
			}

			if (Time.time < _nextScan)
			{
				return;
			}

			_nextScan = Time.time + ScanInterval;
			ScanEncounters(player);
		}

		private void AddLesson(Lesson lesson, string id, string description)
		{
			TutorialDefinition definition = TutorialDefinition.CreateRuntime(id,
				new TutorialStep(id).WithDescriptionKey(description));
			if (_tutorials.IsFinished(definition))
			{
				UnityEngine.Object.Destroy(definition);
				return;
			}

			_lessons[(int)lesson] = definition;
			_lessonsRemaining++;
		}

		private void ScanEncounters(Player player)
		{
			Vector2 position = player.transform.position;
			UnityEngine.Camera camera = UnityEngine.Camera.main;
			if (camera == null)
			{
				return;
			}

			MapFeatureField.Anchor hole = MapFeatureField.Anchors[MapFeatureField.BlackHoleSlot];
			float holeNoticeDistance = hole.ZoneRadius + 2f;
			if (IsPending(Lesson.BlackHole) && hole.Radius > 0f &&
				IsNear(hole.Position, position, holeNoticeDistance) && TryShow(Lesson.BlackHole))
			{
				return;
			}

			if (IsPending(Lesson.Comet))
			{
				Comet[] comets = UnityEngine.Object.FindObjectsByType<Comet>(FindObjectsSortMode.None);
				foreach (Comet comet in comets)
				{
					if (comet.IsSpawned && IsNear(comet.transform.position, position, CometNoticeDistance) &&
						IsVisible(camera, comet.transform.position) && TryShow(Lesson.Comet))
					{
						return;
					}
				}
			}

			if (IsPending(Lesson.Food) && _food.World != null)
			{
				_food.Query(position, FoodNoticeDistance, _nearbyFood);
				foreach (FoodRecord food in _nearbyFood)
				{
					if (IsVisible(camera, food.Position) && TryShow(Lesson.Food))
					{
						return;
					}
				}
			}

			if (IsPending(Lesson.Bot))
			{
				foreach (PlanetScale planet in PlanetDirectory.All)
				{
					if (planet is Enemy enemy && enemy.IsSpawned && enemy.AreMoonsVisible &&
						IsNear(enemy.transform.position, position, BotNoticeDistance) &&
						IsVisible(camera, enemy.transform.position) && TryShow(Lesson.Bot))
					{
						return;
					}
				}
			}
		}

		private static bool IsNear(Vector2 target, Vector2 position, float range)
		{
			return (target - position).sqrMagnitude <= range * range;
		}

		private static bool IsVisible(UnityEngine.Camera camera, Vector3 position)
		{
			Vector3 point = camera.WorldToViewportPoint(position);

			return point.z > 0f && point.x >= 0f && point.x <= 1f && point.y >= 0f && point.y <= 1f;
		}

		private bool IsPending(Lesson lesson) => _lessons[(int)lesson] != null;

		private bool TryShow(Lesson lesson)
		{
			TutorialDefinition definition = _lessons[(int)lesson];

			return definition != null && _tutorials.Start(definition);
		}

		private void OnProgressionChanged()
		{
			int rank = _progression.Rank;
			_rankIncreased |= rank > _lastRank;
			_lastRank = rank;
		}

		private void OnLessonStarted(TutorialDefinition _) => _pause = _pauses.RequestPause("tutorial");

		private void RenderLesson(TutorialStepState state)
		{
			_hud.ShowTutorial(
				_localization.Get(state.Step.DescriptionKey),
				_localization.Get(LocalizationKeys.TutorialContinue),
				_localization.Get(LocalizationKeys.TutorialSkip));
		}

		private void OnLocaleChanged(LocaleChanged _)
		{
			if (_tutorials.TryGetCurrentStep(out TutorialStepState state))
			{
				RenderLesson(state);
			}
		}

		private void OnContinue()
		{
			if (_tutorials.IsActive)
			{
				_tutorials.CompleteCurrentStep();
			}
		}

		private void OnSkip()
		{
			if (_tutorials.IsActive)
			{
				_tutorials.Skip();
			}
		}

		private void OnLessonEnded(TutorialEnded ended)
		{
			_hud.HideTutorial();
			_pause?.Dispose();
			_pause = null;

			if (ended.Reason == TutorialEndReason.Cancelled)
			{
				return;
			}

			int index = Array.IndexOf(_lessons, ended.Tutorial);
			_analytics.Track(AnalyticsEvents.TutorialStep, ("step", ended.Tutorial.Id),
				("skipped", ended.Reason == TutorialEndReason.Skipped ? 1 : 0));

			_lessons[index] = null;
			UnityEngine.Object.Destroy(ended.Tutorial);
			_lessonsRemaining--;

			if (ended.Reason == TutorialEndReason.Skipped || _lessonsRemaining == 0)
			{
				_lessonsRemaining = 0;
				_settings.Set(GameSettingKeys.TutorialCompleted, true);
				_settings.Flush();
			}
		}
	}

	public sealed partial class SessionHudView
	{
		private VisualElement _tutorialOverlay;
		private Label _tutorialDescription;
		private Button _tutorialContinue;
		private Button _tutorialSkip;

		public event Action TutorialContinued;
		public event Action TutorialSkipped;

		public bool IsShowingDefeat => _defeatVisible;

		public bool CanShowTutorial => !_defeatVisible && _root?.panel != null;

		private void EnableTutorial()
		{
			_tutorialOverlay = _root.Q("tutorial-overlay");
			_tutorialDescription = _root.Q<Label>("tutorial-description");
			_tutorialContinue = _root.Q<Button>("tutorial-continue");
			_tutorialSkip = _root.Q<Button>("tutorial-skip");
			_tutorialContinue.clicked += OnTutorialContinued;
			_tutorialSkip.clicked += OnTutorialSkipped;
		}

		private void DisableTutorial()
		{
			_tutorialContinue.clicked -= OnTutorialContinued;
			_tutorialSkip.clicked -= OnTutorialSkipped;
			HideTutorial();
		}

		private void OnTutorialContinued() => TutorialContinued?.Invoke();

		private void OnTutorialSkipped() => TutorialSkipped?.Invoke();

		public void ShowTutorial(string description, string continueLabel, string skipLabel)
		{
			_tutorialDescription.text = description;
			_tutorialContinue.text = continueLabel;
			_tutorialSkip.text = skipLabel;
			_tutorialOverlay.style.display = DisplayStyle.Flex;
			_tutorialContinue.Focus();
		}

		public void HideTutorial() => _tutorialOverlay.style.display = DisplayStyle.None;
	}
}
