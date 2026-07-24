using System;
using VContainer.Unity;

namespace PlanetIO.UI.Loading
{
    public sealed class LoadingPresenter : IStartable, IDisposable
    {
        private readonly ILoadingView _loadingView;
        private readonly INetworkSessionService _networkSessionService;

        public LoadingPresenter(ILoadingView loadingView, INetworkSessionService networkSessionService)
        {
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

        private void OnLoadingProgressChanged(float progress)
        {
            Render();
        }

        private void OnSessionStateChanged(NetworkSessionState state, string status)
		{
            Render();
        }

        private void Render()
        {
            _loadingView.Render(_networkSessionService.LoadingProgress, _networkSessionService.Status);
        }
    }
}
