using System;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace PlanetIO.Infrastructure.Networking
{
    public sealed class UgsSessionConnector
    {
        private static readonly TimeSpan QuickJoinTimeout = TimeSpan.FromSeconds(3);

        private bool _initialized;

        public ISession ActiveSession { get; private set; }

        public async Awaitable<ISession> CreatePrivateRoomAsync(int maxPlayers, INetworkHandler networkHandler)
        {
            await EnsureInitializedAsync();
            SessionOptions options = new SessionOptions { MaxPlayers = maxPlayers, IsPrivate = true }
                .WithRelayNetwork()
                .WithNetworkHandler(networkHandler);

            ActiveSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            return ActiveSession;
        }

        public async Awaitable<ISession> JoinByCodeAsync(string code, INetworkHandler networkHandler)
        {
            await EnsureInitializedAsync();
            JoinSessionOptions options = new JoinSessionOptions().WithNetworkHandler(networkHandler);

            ActiveSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(code, options);
            return ActiveSession;
        }

        public async Awaitable<ISession> QuickPlayAsync(int maxPlayers, INetworkHandler networkHandler)
        {
            await EnsureInitializedAsync();
            QuickJoinOptions quickJoin = new() { Timeout = QuickJoinTimeout, CreateSession = true };
            SessionOptions options = new SessionOptions { MaxPlayers = maxPlayers }
                .WithRelayNetwork()
                .WithNetworkHandler(networkHandler);

            ActiveSession = await MultiplayerService.Instance.MatchmakeSessionAsync(quickJoin, options);
            return ActiveSession;
        }

        public async Awaitable LeaveAsync()
        {
            ISession session = ActiveSession;
            ActiveSession = null;
            if (session == null)
            {
                return;
            }

            try
            {
                await session.LeaveAsync();
            }
            catch (Exception exception)
            {
                GameLogger.LogWarning($"Leaving session failed: {exception.Message}");
            }
        }

        private async Awaitable EnsureInitializedAsync()
        {
            if (_initialized)
            {
                return;
            }

            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            _initialized = true;
        }
    }
}
