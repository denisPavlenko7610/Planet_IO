using System;
using System.Collections.Generic;
using System.Text;
using PlanetIO;
using PlanetIO.Utils;
using PlanetIO.UI.Mobile;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace PlanetIO.UI.Hud
{
    public sealed class GameSessionHudPresenter :
        IStartable,
        ITickable,
        IDisposable
    {
        private const float RefreshIntervalSeconds = 0.5f;
        private const int VisibleLeaderboardEntries = 6;

        private readonly NetworkManager _networkManager;
        private readonly INetworkSessionService _networkSessionService;
        private readonly List<LeaderboardEntry> _entries = new();
        private GameObject _viewRoot;
        private TMP_Text _sessionText;
        private TMP_Text _leaderboardText;
        private Button _leaveButton;
        private float _refreshTimeRemaining;
        private bool _leaveInProgress;

        public GameSessionHudPresenter(
            NetworkManager networkManager,
            INetworkSessionService networkSessionService)
        {
            _networkManager = networkManager
                ?? throw new ArgumentNullException(nameof(networkManager));
            _networkSessionService = networkSessionService
                ?? throw new ArgumentNullException(
                    nameof(networkSessionService));
        }

        public void Start()
        {
            BuildView();
            Refresh();
        }

        public void Tick()
        {
            _refreshTimeRemaining -= Time.unscaledDeltaTime;
            if (_refreshTimeRemaining > 0f)
            {
                return;
            }

            _refreshTimeRemaining = RefreshIntervalSeconds;
            Refresh();
        }

        public void Dispose()
        {
            if (_leaveButton != null)
            {
                _leaveButton.onClick.RemoveListener(OnLeaveRequested);
            }

            if (_viewRoot != null)
            {
                Object.Destroy(_viewRoot);
            }
        }

        private void BuildView()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("Game HUD canvas was not found.");
                return;
            }

            _viewRoot = new GameObject(
                "SessionHud",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform panel = _viewRoot.GetComponent<RectTransform>();
            panel.SetParent(canvas.transform, false);
            panel.anchorMin = Vector2.one;
            panel.anchorMax = Vector2.one;
            panel.pivot = Vector2.one;
            panel.anchoredPosition = new Vector2(-28f, -28f);
            panel.sizeDelta = new Vector2(470f, 390f);
            _viewRoot.GetComponent<Image>().color =
                new Color(0.025f, 0.055f, 0.12f, 0.82f);
            SafeAreaElement.AttachTo(panel);

            _sessionText = CreateText(
                "Session",
                panel,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(22f, -18f),
                new Vector2(-22f, -78f),
                28f,
                FontStyles.Bold);

            _leaderboardText = CreateText(
                "Leaderboard",
                panel,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(24f, 82f),
                new Vector2(-24f, -88f),
                25f,
                FontStyles.Normal);
            _leaderboardText.alignment =
                TextAlignmentOptions.TopLeft;

            _leaveButton = CreateButton(panel);
            _leaveButton.onClick.AddListener(OnLeaveRequested);
        }

        private void Refresh()
        {
            if (_sessionText == null || _leaderboardText == null)
            {
                return;
            }

            RoomConnectionSettings room =
                _networkSessionService.CurrentRoom;
            string roomLabel = _networkSessionService.Mode ==
                               NetworkSessionMode.SinglePlayer
                ? "SINGLE PLAYER"
                : $"ROOM {room.RoomCode}";
            int playerCount =
                _networkManager.ConnectedClientsList?.Count ?? 0;
            _sessionText.text =
                $"{roomLabel}\nPlayers: {playerCount}/{room.MaxPlayers}";

            CollectEntries();
            _entries.Sort(static (left, right) =>
                right.Score.CompareTo(left.Score));

            StringBuilder builder = new();
            builder.AppendLine("<b>LEADERS</b>");
            int visibleCount = Mathf.Min(
                VisibleLeaderboardEntries,
                _entries.Count);

            for (int index = 0; index < visibleCount; index++)
            {
                LeaderboardEntry entry = _entries[index];
                string line =
                    $"{index + 1}. {EscapeRichText(entry.Name)}" +
                    $"  {entry.Score:N0}";
                builder.Append(entry.IsLocalPlayer
                    ? $"<color=#5FE0FF>{line}</color>"
                    : line);

                if (index < visibleCount - 1)
                {
                    builder.AppendLine();
                }
            }

            _leaderboardText.text = builder.ToString();
        }

        private void CollectEntries()
        {
            _entries.Clear();
            if (_networkManager.SpawnManager == null)
            {
                return;
            }

            foreach (NetworkObject networkObject in
                     _networkManager.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject.TryGetComponent(out Player player))
                {
                    string nickname =
                        networkObject.TryGetComponent(
                            out PlayerNickname playerNickname)
                            ? playerNickname.Nickname
                            : $"Player {networkObject.OwnerClientId}";
                    _entries.Add(new LeaderboardEntry(
                        nickname,
                        ToScore(player.Capacity),
                        networkObject.IsOwner));
                }
                else if (networkObject.TryGetComponent(out Enemy enemy))
                {
                    _entries.Add(new LeaderboardEntry(
                        $"Bot {networkObject.NetworkObjectId % 100:00}",
                        ToScore(enemy.Capacity),
                        false));
                }
            }
        }

        private void OnLeaveRequested()
        {
            _ = LeaveAsync();
        }

        private async Awaitable LeaveAsync()
        {
            if (_leaveInProgress)
            {
                return;
            }

            _leaveInProgress = true;
            if (_leaveButton != null)
            {
                _leaveButton.interactable = false;
            }

            try
            {
                await _networkSessionService
                    .ShutdownAndReturnToMenuAsync();
            }
            catch (OperationCanceledException)
            {
                // Scene or application is closing.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                _leaveInProgress = false;
                if (_leaveButton != null)
                {
                    _leaveButton.interactable = true;
                }
            }
        }

        private static int ToScore(float capacity) =>
            Constants.CapacityToScore(capacity);

        private static string EscapeRichText(string value) =>
            (value ?? NicknameRules.DefaultNickname)
            .Replace("<", "‹")
            .Replace(">", "›");

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            float fontSize,
            FontStyles fontStyle)
        {
            GameObject gameObject = new(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            RectTransform rectTransform =
                gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;

            TMP_Text text = gameObject.GetComponent<TMP_Text>();
            text.color = Color.white;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true;
            text.fontSizeMin = 16f;
            text.fontSizeMax = fontSize;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent)
        {
            GameObject buttonObject = new(
                "LeaveButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            RectTransform rectTransform =
                buttonObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.offsetMin = new Vector2(22f, 18f);
            rectTransform.offsetMax = new Vector2(-22f, 72f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.11f, 0.48f, 0.72f, 1f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            TMP_Text label = CreateText(
                "Label",
                rectTransform,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                24f,
                FontStyles.Bold);
            label.alignment = TextAlignmentOptions.Center;
            label.text = "MENU";
            return button;
        }

        private readonly struct LeaderboardEntry
        {
            public LeaderboardEntry(
                string name,
                int score,
                bool isLocalPlayer)
            {
                Name = name;
                Score = score;
                IsLocalPlayer = isLocalPlayer;
            }

            public string Name { get; }
            public int Score { get; }
            public bool IsLocalPlayer { get; }
        }
    }
}
