using UnityEngine.UIElements;

namespace PlanetIO.UI.Menu
{
    public sealed partial class MainMenuView
    {
        private VisualElement _customizeOverlay;
        private VisualElement _onlineOverlay;
        private VisualElement _customizeBackdrop;
        private VisualElement _onlineBackdrop;
        private Button _profileButton;
        private Button _onlineButton;
        private Button _customizeClose;
        private Button _onlineClose;
        private Label _onlineStatus;

        private void EnableScreens(VisualElement root)
        {
            _customizeOverlay = root.Q<VisualElement>("customize-overlay");
            _onlineOverlay = root.Q<VisualElement>("online-overlay");
            _customizeBackdrop = root.Q<VisualElement>("customize-backdrop");
            _onlineBackdrop = root.Q<VisualElement>("online-backdrop");
            _profileButton = root.Q<Button>("profile-button");
            _onlineButton = root.Q<Button>("online-button");
            _customizeClose = root.Q<Button>("customize-close");
            _onlineClose = root.Q<Button>("online-close");
            _onlineStatus = root.Q<Label>("online-status");

            SetScreenVisible(_customizeOverlay, false);
            SetScreenVisible(_onlineOverlay, false);

            _profileButton.clicked += OnProfileClicked;
            _onlineButton.clicked += OnOnlineClicked;
            _customizeClose.clicked += OnCustomizeCloseClicked;
            _onlineClose.clicked += OnOnlineCloseClicked;
            _customizeBackdrop.RegisterCallback<ClickEvent>(OnCustomizeBackdropClicked);
            _onlineBackdrop.RegisterCallback<ClickEvent>(OnOnlineBackdropClicked);
        }

        private void DisableScreens()
        {
            _profileButton.clicked -= OnProfileClicked;
            _onlineButton.clicked -= OnOnlineClicked;
            _customizeClose.clicked -= OnCustomizeCloseClicked;
            _onlineClose.clicked -= OnOnlineCloseClicked;
            _customizeBackdrop.UnregisterCallback<ClickEvent>(OnCustomizeBackdropClicked);
            _onlineBackdrop.UnregisterCallback<ClickEvent>(OnOnlineBackdropClicked);
        }

        private void MirrorStatus(string status, bool isError)
        {
            _onlineStatus.text = status ?? string.Empty;
            _onlineStatus.EnableInClassList(ErrorClass, isError);
        }

        private static void SetScreenVisible(VisualElement overlay, bool visible)
        {
            overlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnProfileClicked() => SetScreenVisible(_customizeOverlay, true);

        private void OnOnlineClicked() => SetScreenVisible(_onlineOverlay, true);

        private void OnCustomizeCloseClicked() => SetScreenVisible(_customizeOverlay, false);

        private void OnOnlineCloseClicked() => SetScreenVisible(_onlineOverlay, false);

        private void OnCustomizeBackdropClicked(ClickEvent clickEvent) => SetScreenVisible(_customizeOverlay, false);

        private void OnOnlineBackdropClicked(ClickEvent clickEvent) => SetScreenVisible(_onlineOverlay, false);
    }
}
