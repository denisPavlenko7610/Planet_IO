namespace PlanetIO
{
    public sealed partial class EnemyMovement
    {
        private MatchMigrationStore _migration;
        [VContainer.Inject] public void ConfigureMigration(MatchMigrationStore store) => _migration = store;

        public void CaptureMigration(PlanetMigrationState s)
        {
            s.Personality = (int)Personality;
            s.Direction = Direction;
            s.DesiredDirection = _desiredDirection;
            s.MovementState = (int)State;
            s.StateRemaining = _stateTimeRemaining;
            s.ThinkRemaining = _thinkTimeRemaining;
            s.DashRemaining = _dashTimeRemaining;
            s.DashDelay = _nextDashDelay;
            s.DashEnabled = _dashEnabled;
            s.BoostRemaining = _boostRemaining;
            s.BoostCooldown = _boostCooldown;
            s.BoostTimer = _boostTick;
            s.Boosting = IsBoosting;
            s.TargetId = _targetPlanet != null ? _targetPlanet.NetworkObjectId : ulong.MaxValue;
        }

        public void RestoreMigration(PlanetMigrationState s)
        {
            ApplyPersonality((BotPersonality)s.Personality);
            ApplyDifficulty();
            Direction = s.Direction;
            _desiredDirection = s.DesiredDirection;
            State = (MovementState)s.MovementState;
            _stateTimeRemaining = s.StateRemaining;
            _thinkTimeRemaining = s.ThinkRemaining;
            _dashTimeRemaining = s.DashRemaining;
            _nextDashDelay = s.DashDelay;
            _dashEnabled = s.DashEnabled;
            _boostRemaining = s.BoostRemaining;
            _boostCooldown = s.BoostCooldown;
            _boostTick = s.BoostTimer;
            _targetPlanet = null;
            _enemy.SetBoosting(s.Boosting);
        }
    }
}
