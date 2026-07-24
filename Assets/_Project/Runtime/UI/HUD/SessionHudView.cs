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
        void SetLeaveButtonInteractable(bool interactable);
    }

    public sealed class SessionHudView : MonoBehaviour, ISessionHudView
    {
        [SerializeField] private TMP_Text _sessionText;
        [SerializeField] private TMP_Text _leaderboardText;
        [SerializeField] private Button _leaveButton;

        public event Action LeaveRequested;

        private void Awake()
        {
            _leaveButton.onClick.AddListener(OnLeaveClicked);
        }

        private void OnDestroy()
        {
            _leaveButton.onClick.RemoveListener(OnLeaveClicked);
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
