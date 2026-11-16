using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlanetIO.UI.Menu
{
    public enum MissionRowState : byte
    {
        InProgress,
        Claimable,
        Claimed
    }

    public readonly struct MissionRow
    {
        public readonly string Title;
        public readonly string Progress;
        public readonly string ActionLabel;
        public readonly string RerollLabel;
        public readonly MissionRowState State;
        public readonly bool CanReroll;

        public MissionRow(
            string title,
            string progress,
            string actionLabel,
            string rerollLabel,
            MissionRowState state,
            bool canReroll)
        {
            Title = title;
            Progress = progress;
            ActionLabel = actionLabel;
            RerollLabel = rerollLabel;
            State = state;
            CanReroll = canReroll;
        }
    }

    public interface IProgressView
    {
        event Action MissionsRequested;
        event Action MissionsClosed;
        event Action<int> MissionClaimRequested;
        event Action<int> MissionRerollRequested;

        void ShowRankAndDust(string text);
        void SetMissionsAlert(bool visible);
        void ShowMissions(string title, string streak, string bonus, string close, MissionRow[] rows);
        void SetMissionsVisible(bool visible);
    }

    public sealed partial class MainMenuView : IProgressView
    {
        private const int MissionSlots = MissionRules.DailyCount;

        private readonly Label[] _missionNames = new Label[MissionSlots];
        private readonly Label[] _missionProgress = new Label[MissionSlots];
        private readonly Button[] _missionClaims = new Button[MissionSlots];
        private readonly Button[] _missionRerolls = new Button[MissionSlots];
        private readonly Action[] _claimHandlers = new Action[MissionSlots];
        private readonly Action[] _rerollHandlers = new Action[MissionSlots];
        private VisualElement _missionsOverlay;
        private Label _rankDustText;
        private Label _missionsTitle;
        private Label _missionsStreak;
        private Label _missionsBonus;
        private Button _missionsButton;
        private Label _missionsBadge;
        private Button _missionsClose;

        public event Action MissionsRequested;
        public event Action MissionsClosed;
        public event Action<int> MissionClaimRequested;
        public event Action<int> MissionRerollRequested;

        private void EnableProgressView(VisualElement root)
        {
            _rankDustText = root.Q<Label>("rank-dust");
            _missionsOverlay = root.Q<VisualElement>("missions-overlay");
            _missionsTitle = root.Q<Label>("missions-title");
            _missionsStreak = root.Q<Label>("missions-streak");
            _missionsBonus = root.Q<Label>("missions-bonus");
            _missionsButton = root.Q<Button>("missions-button");
            _missionsBadge = root.Q<Label>("missions-badge");
            _missionsClose = root.Q<Button>("missions-close");
            _missionsOverlay.style.display = DisplayStyle.None;

            _missionsButton.clicked += OnMissionsClicked;
            _missionsClose.clicked += OnMissionsCloseClicked;

            for (int index = 0; index < MissionSlots; index++)
            {
                int slot = index;
                _missionNames[index] = root.Q<Label>($"mission-name-{index}");
                _missionProgress[index] = root.Q<Label>($"mission-progress-{index}");
                _missionClaims[index] = root.Q<Button>($"mission-claim-{index}");
                _missionRerolls[index] = root.Q<Button>($"mission-reroll-{index}");
                _claimHandlers[index] = () => MissionClaimRequested?.Invoke(slot);
                _rerollHandlers[index] = () => MissionRerollRequested?.Invoke(slot);
                _missionClaims[index].clicked += _claimHandlers[index];
                _missionRerolls[index].clicked += _rerollHandlers[index];
            }
        }

        private void DisableProgressView()
        {
            _missionsButton.clicked -= OnMissionsClicked;
            _missionsClose.clicked -= OnMissionsCloseClicked;
            for (int index = 0; index < MissionSlots; index++)
            {
                _missionClaims[index].clicked -= _claimHandlers[index];
                _missionRerolls[index].clicked -= _rerollHandlers[index];
            }
        }

        public void ShowRankAndDust(string text)
        {
            _rankDustText.text = text ?? string.Empty;
        }

        public void SetMissionsAlert(bool visible)
        {
            _missionsBadge.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void ShowMissions(string title, string streak, string bonus, string close, MissionRow[] rows)
        {
            _missionsTitle.text = title;
            _missionsStreak.text = streak;
            _missionsBonus.text = bonus ?? string.Empty;
            _missionsBonus.style.display = string.IsNullOrEmpty(bonus) ? DisplayStyle.None : DisplayStyle.Flex;
            _missionsClose.text = close;

            for (int index = 0; index < MissionSlots; index++)
            {
                bool available = rows != null && index < rows.Length;
                VisualElement row = _missionNames[index].parent.parent;
                row.style.display = available ? DisplayStyle.Flex : DisplayStyle.None;
                if (!available)
                {
                    continue;
                }

                MissionRow data = rows[index];
                _missionNames[index].text = data.Title;
                _missionProgress[index].text = data.Progress;
                _missionClaims[index].text = data.ActionLabel;
                _missionClaims[index].SetEnabled(data.State == MissionRowState.Claimable);
                _missionClaims[index].style.opacity = data.State == MissionRowState.InProgress ? 0.45f : 1f;
                _missionRerolls[index].text = data.RerollLabel;
                _missionRerolls[index].style.display = data.CanReroll ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void SetMissionsVisible(bool visible)
        {
            _missionsOverlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnMissionsClicked() => MissionsRequested?.Invoke();

        private void OnMissionsCloseClicked() => MissionsClosed?.Invoke();
    }
}
