using System;
using UnityEngine.UIElements;

namespace PlanetIO.UI.Hud
{
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
