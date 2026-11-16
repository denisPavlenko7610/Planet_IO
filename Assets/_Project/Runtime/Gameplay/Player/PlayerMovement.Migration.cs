namespace PlanetIO
{
    public sealed partial class PlayerMovement
    {
        private PlanetMigrationState _migrationMovement;

        public void CaptureMigration(PlanetMigrationState s)
        {
            s.Direction = Direction;
            s.DesiredDirection = _desiredDirection;
        }

        public void RestoreMigration(PlanetMigrationState s)
        {
            Direction = s.Direction;
            _desiredDirection = s.DesiredDirection;
            if (IsServer && !IsOwner)
            {
                _migrationMovement = s;
            }
        }

        private void RestoreOwnerMovement()
        {
            if (!IsOwner || !_player.WasMigrationRestored)
            {
                return;
            }

            float angle = transform.eulerAngles.z * UnityEngine.Mathf.Deg2Rad;
            Direction = new UnityEngine.Vector2(UnityEngine.Mathf.Cos(angle), UnityEngine.Mathf.Sin(angle));
            _desiredDirection = Direction;
        }
    }
}
