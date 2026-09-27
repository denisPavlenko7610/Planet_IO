using System;
using VContainer.Unity;
using UnityTemplates.Localization;

namespace PlanetIO.UI.Loading
{
    public sealed class LoadingPresenter : IStartable, IDisposable
    {
        private readonly ILoadingView _loadingView;
        private readonly INetworkSessionService _networkSessionService;
        private readonly ILocalizationService _localization;

        public LoadingPresenter(ILoadingView loadingView, INetworkSessionService networkSessionService, ILocalizationService localization)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _loadingView = loadingView ?? throw new ArgumentNullException(nameof(loadingView));
            _networkSessionService = networkSessionService ?? throw new ArgumentNullException(nameof(networkSessionService));
        }

        public void Start()
        {
            _networkSessionService.LoadingProgressChanged += OnLoadingProgressChanged;
            _networkSessionService.StateChanged += OnSessionStateChanged;
            Render();
        }

        public void Dispose()
        {
            _networkSessionService.LoadingProgressChanged -= OnLoadingProgressChanged;
            _networkSessionService.StateChanged -= OnSessionStateChanged;
        }

        private void OnLoadingProgressChanged(float _)
        {
            Render();
        }

        private void OnSessionStateChanged(NetworkSessionState _, string __)
		{
            Render();
        }

        private void Render()
        {
            string status = SessionStatusFormatter.Format(_localization, _networkSessionService.State,
                _networkSessionService.Mode, _networkSessionService.CurrentRoom.RoomCode);
            _loadingView.Render(_networkSessionService.LoadingProgress, status);
        }
    }
}
