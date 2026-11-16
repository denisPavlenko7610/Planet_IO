namespace PlanetIO
{
    public sealed partial class CometMovement
    {
        public CometMigrationState CaptureMigration()
        {
            return new CometMigrationState
            {
                ObjectId = NetworkObjectId, Position = transform.position,
                Direction = _direction, Speed = _normalSpeed, Mass = GetComponent<Comet>().Capacity,
                FoodTimer = _foodTimer, RetryTimer = _retryTimer, Waiting = _waiting
            };
        }

        public void RestoreMigration(CometMigrationState s)
        {
            Launch(s.Position, s.Direction, s.Speed);
            _foodTimer = s.FoodTimer;
            _retryTimer = s.RetryTimer;
            _waiting = s.Waiting;
        }
    }
}
