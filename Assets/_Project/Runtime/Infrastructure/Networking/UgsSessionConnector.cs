using RD.Foundation;
using System;
using System.Collections.Generic;
using System.Globalization;
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

        public IMigrationDataHandler MigrationHandler { get; set; }
        public string LocalIdentity => AuthenticationService.Instance.PlayerId;
        public ISession ActiveSession { get; private set; }
        public const string DifficultyProperty = "bot-difficulty";
        public int RequestedBotDifficulty { get; set; } = (int)BotDifficulty.Medium;

        public static BotDifficulty ReadDifficulty(IReadOnlyDictionary<string, SessionProperty> properties)
        {
            return properties != null && properties.TryGetValue(DifficultyProperty, out SessionProperty property) &&
                property != null && int.TryParse(property.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? (BotDifficulty)BotDifficultyRules.Normalize(value) : BotDifficulty.Medium;
        }

        private Dictionary<string, SessionProperty> CreateProperties() => new()
        {
            [DifficultyProperty] = new SessionProperty(BotDifficultyRules.Normalize(RequestedBotDifficulty)
                .ToString(CultureInfo.InvariantCulture))
        };

        public async Awaitable<ISession> CreatePrivateRoomAsync(int maxPlayers, INetworkHandler networkHandler)
        {
            await EnsureInitializedAsync();
            SessionOptions options = new SessionOptions { MaxPlayers = maxPlayers, IsPrivate = true, SessionProperties = CreateProperties() }
                .WithRelayNetwork()
                .WithNetworkHandler(networkHandler);

            options.WithHostMigration(MigrationHandler, TimeSpan.FromDays(365), TimeSpan.FromSeconds(5));
            ActiveSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            return ActiveSession;
        }

        public async Awaitable<ISession> JoinByCodeAsync(string code, INetworkHandler networkHandler)
        {
            await EnsureInitializedAsync();
            JoinSessionOptions options = new JoinSessionOptions().WithNetworkHandler(networkHandler);

            options.WithHostMigration(MigrationHandler, TimeSpan.FromDays(365), TimeSpan.FromSeconds(5));
            ActiveSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(code, options);
            return ActiveSession;
        }

        public async Awaitable<ISession> QuickPlayAsync(int maxPlayers, INetworkHandler networkHandler)
        {
            await EnsureInitializedAsync();
            QuickJoinOptions quickJoin = new() { Timeout = QuickJoinTimeout, CreateSession = true };
            SessionOptions options = new SessionOptions { MaxPlayers = maxPlayers, SessionProperties = CreateProperties() }
                .WithRelayNetwork()
                .WithNetworkHandler(networkHandler);

            options.WithHostMigration(MigrationHandler, TimeSpan.FromDays(365), TimeSpan.FromSeconds(5));
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
                UnityLogger.LogWarning($"Leaving session failed: {exception.Message}");
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
