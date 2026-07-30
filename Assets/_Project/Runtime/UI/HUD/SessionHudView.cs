using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetIO.UI.Hud
{
    public interface ISessionHudView
    {
        event Action LeaveRequested;

        void ShowSessionText(string text);
        void ShowLeaderboardText(string text);
        void ShowDefeat(int finalScore);
        void SetLeaveButtonInteractable(bool interactable);
    }

    public sealed class SessionHudView : MonoBehaviour, ISessionHudView
    {
        private const string DefeatTitle = "YOU LOST";

        [SerializeField] private TMP_Text _sessionText;
        [SerializeField] private TMP_Text _leaderboardText;
        [SerializeField] private Button _leaveButton;

        public event Action LeaveRequested;
        public bool IsDefeatVisible =>
            _sessionText != null &&
            _sessionText.text == DefeatTitle;

        private void OnEnable()
        {
            _leaveButton?.onClick.AddListener(OnLeaveClicked);
        }

        private void OnDisable()
        {
            _leaveButton?.onClick.RemoveListener(OnLeaveClicked);
        }

        public void ShowSessionText(string text)
        {
            if (_sessionText != null)
            {
                _sessionText.text = text;
            }
        }

        public void ShowLeaderboardText(string text)
        {
            if (_leaderboardText != null)
            {
                _leaderboardText.text = text;
            }
        }

        public void ShowDefeat(int finalScore)
        {
            ShowSessionText(DefeatTitle);
            ShowLeaderboardText($"FINAL SCORE\n{finalScore:N0}");
        }

        public void SetLeaveButtonInteractable(bool interactable)
        {
            if (_leaveButton != null)
            {
                _leaveButton.interactable = interactable;
            }
        }

        private void OnLeaveClicked()
        {
            LeaveRequested?.Invoke();
        }
    }
}
