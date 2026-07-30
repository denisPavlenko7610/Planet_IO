using System;
using UnityEngine;
using VContainer.Unity;

namespace PlanetIO.UI.Menu
{
    public sealed class MenuPresenter : IStartable, IDisposable
    {
        private readonly INetworkMenuView _networkMenuView;
        private readonly INicknameInputView _nicknameInputView;
        private readonly INetworkSessionService _networkSessionService;
        private readonly IPlayerProfileService _playerProfileService;
        private bool _sessionRequestInProgress;

        public MenuPresenter(INetworkMenuView networkMenuView, INicknameInputView nicknameInputView, INetworkSessionService networkSessionService,
            IPlayerProfileService playerProfileService)
        {
            _networkMenuView = networkMenuView ?? throw new ArgumentNullException(nameof(networkMenuView));
            _nicknameInputView = nicknameInputView ?? throw new ArgumentNullException(nameof(nicknameInputView));
            _networkSessionService = networkSessionService ?? throw new ArgumentNullException(nameof(networkSessionService));
            _playerProfileService = playerProfileService ?? throw new ArgumentNullException(nameof(playerProfileService));
        }

        public void Start()
        {
            _networkMenuView.HostRequested += OnHostRequested;
            _networkMenuView.JoinRequested += OnJoinRequested;
            _networkMenuView.SinglePlayerRequested += OnSinglePlayerRequested;
            _nicknameInputView.NicknameChanged += OnNicknameChanged;
            _nicknameInputView.RandomNicknameRequested += OnRandomNicknameRequested;
            _playerProfileService.NicknameChanged += OnProfileNicknameChanged;
            _networkSessionService.StateChanged += OnSessionStateChanged;

            _nicknameInputView.ShowNickname(_playerProfileService.Nickname);
            _networkMenuView.SetInteractionEnabled(true);
            _networkMenuView.ShowStatus(_networkSessionService.Status, false);
        }

        public void Dispose()
        {
            _networkMenuView.HostRequested -= OnHostRequested;
            _networkMenuView.JoinRequested -= OnJoinRequested;
            _networkMenuView.SinglePlayerRequested -= OnSinglePlayerRequested;
            _nicknameInputView.NicknameChanged -= OnNicknameChanged;
            _nicknameInputView.RandomNicknameRequested -= OnRandomNicknameRequested;
            _playerProfileService.NicknameChanged -= OnProfileNicknameChanged;
            _networkSessionService.StateChanged -= OnSessionStateChanged;
        }

        private void OnHostRequested()
        {
            _ = StartSessionAsync(
				() => _networkSessionService.StartHostAsync(RoomRules.DefaultMaxPlayers), "Failed to start host");
        }

        private void OnJoinRequested(string relayJoinCode)
        {
            _ = StartSessionAsync(
                () => _networkSessionService.StartClientAsync(relayJoinCode), "Failed to connect to room");
        }

        private void OnSinglePlayerRequested()
        {
            _ = StartSessionAsync(_networkSessionService.StartSinglePlayerAsync, "Failed to start single player");
        }

        private void OnNicknameChanged(string nickname)
        {
            _playerProfileService.SetNickname(nickname);
        }

        private void OnRandomNicknameRequested()
        {
            _playerProfileService.SetRandomNickname();
        }

        private void OnProfileNicknameChanged(string nickname)
        {
            _nicknameInputView.ShowNickname(nickname);
        }

        private void OnSessionStateChanged(NetworkSessionState state, string status)
        {
            bool isError = state == NetworkSessionState.Failed;
            _networkMenuView.ShowStatus(status, isError);

            if (isError)
            {
                RestoreInteraction();
            }
        }

        private async Awaitable StartSessionAsync(Func<Awaitable<bool>> startSession, string failureMessage)
        {
            if (_sessionRequestInProgress)
            {
                return;
            }

            _sessionRequestInProgress = true;
            _networkMenuView.SetInteractionEnabled(false);

            try
            {
                bool sessionStarted = await startSession();
                if (sessionStarted)
                {
                    return;
                }

                RestoreInteraction();
                LoggerIO.LogError($"{failureMessage}: {_networkSessionService.Status}");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                RestoreInteraction();
                LoggerIO.LogException(exception);
            }
        }

        private void RestoreInteraction()
        {
            _sessionRequestInProgress = false;
            _networkMenuView.SetInteractionEnabled(true);
        }
    }
}
