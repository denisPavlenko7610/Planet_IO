using System;
using Planet_IO;
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

        public MenuPresenter(
            INetworkMenuView networkMenuView,
            INicknameInputView nicknameInputView,
            INetworkSessionService networkSessionService,
            IPlayerProfileService playerProfileService)
        {
            _networkMenuView = networkMenuView
                ?? throw new ArgumentNullException(nameof(networkMenuView));
            _nicknameInputView = nicknameInputView
                ?? throw new ArgumentNullException(nameof(nicknameInputView));
            _networkSessionService = networkSessionService
                ?? throw new ArgumentNullException(nameof(networkSessionService));
            _playerProfileService = playerProfileService
                ?? throw new ArgumentNullException(nameof(playerProfileService));
        }

        public void Start()
        {
            _networkMenuView.HostRequested += OnHostRequested;
            _networkMenuView.ClientRequested += OnClientRequested;
            _nicknameInputView.NicknameChanged += OnNicknameChanged;
            _nicknameInputView.RandomNicknameRequested +=
                OnRandomNicknameRequested;
            _playerProfileService.NicknameChanged += OnProfileNicknameChanged;

            _nicknameInputView.ShowNickname(_playerProfileService.Nickname);
            _networkMenuView.SetInteractionEnabled(true);
        }

        public void Dispose()
        {
            _networkMenuView.HostRequested -= OnHostRequested;
            _networkMenuView.ClientRequested -= OnClientRequested;
            _nicknameInputView.NicknameChanged -= OnNicknameChanged;
            _nicknameInputView.RandomNicknameRequested -=
                OnRandomNicknameRequested;
            _playerProfileService.NicknameChanged -= OnProfileNicknameChanged;
        }

        private void OnHostRequested()
        {
            _ = StartSessionAsync(
                _networkSessionService.StartHostAsync,
                "Не удалось запустить хост");
        }

        private void OnClientRequested()
        {
            _ = StartSessionAsync(
                _networkSessionService.StartClientOrSinglePlayerAsync,
                "Не удалось подключиться или запустить одиночную игру");
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

        private async Awaitable StartSessionAsync(
            Func<Awaitable<bool>> startSession,
            string failureMessage)
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
                Debug.LogError(
                    $"{failureMessage}: {_networkSessionService.Status}");
            }
            catch (OperationCanceledException)
            {
                // The menu or application is closing.
            }
            catch (Exception exception)
            {
                RestoreInteraction();
                Debug.LogException(exception);
            }
        }

        private void RestoreInteraction()
        {
            _sessionRequestInProgress = false;
            _networkMenuView.SetInteractionEnabled(true);
        }
    }
}
