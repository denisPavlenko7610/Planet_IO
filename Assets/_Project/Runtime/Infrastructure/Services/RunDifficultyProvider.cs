using System;
using RD.Settings;

namespace PlanetIO.Infrastructure.Services
{
    public sealed class RunDifficultyProvider : IRunDifficultyProvider
    {
        private readonly ISettingsService _settings;
        private readonly INetworkSessionService _session;

        public RunDifficultyProvider(ISettingsService settings, INetworkSessionService session)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public BotDifficulty Current => BotDifficultyRules.Resolve(
            _settings.Get(GameSettingKeys.BotDifficulty),
            _session.Mode,
            _session.CurrentRoom.Difficulty);
    }
}
