using UnityEngine;

namespace PlanetIO
{
    public abstract partial class PlanetBody
    {
        public virtual PlanetMigrationState CaptureMigration()
        {
            var state = new PlanetMigrationState
            {
                ObjectId = NetworkObjectId, Name = DisplayName, Position = transform.position,
                Rotation = transform.rotation, Mass = Capacity, Tier = Tier, MoonPhase = MoonPhaseDegrees,
                MoonImmunity = _moonImmunityRemaining, ClashCooldown = _clashCooldown
            };
            if (TryGetComponent(out Rigidbody2D body))
            {
                state.Velocity = body.linearVelocity;
            }
            _moons.CaptureMigration(state);
            return state;
        }

        protected void RestorePlanetMigration(PlanetMigrationState state)
        {
			_targetScale = PlanetSizeRules.Scale(state.Mass);
            _localCapacity = state.Mass;
            _networkCapacity.Value = state.Mass;
            transform.localScale = new Vector3(
                PlanetSizeRules.Scale(state.Mass),
                PlanetSizeRules.Scale(state.Mass),
                1f
            );
            transform.SetPositionAndRotation(state.Position, state.Rotation);
            _networkTier.Value = (byte)state.Tier;
            _migrationMoonPhase.Value = state.MoonPhase;
            _moons.RestoreMigration(state);
            _moonImmunityRemaining = state.MoonImmunity;
            _clashCooldown = state.ClashCooldown;
            PublishMoonMask();
            SetDisplayName(state.Name);
            if (TryGetComponent(out Rigidbody2D body))
            {
                body.position = state.Position;
                body.rotation = state.Rotation.eulerAngles.z;
                body.linearVelocity = state.Velocity;
            }
            if (IsSpawned &&
                TryGetComponent(out Unity.Netcode.Components.NetworkTransform nt) &&
                nt.CanCommitToTransform)
            {
                nt.Teleport(state.Position, state.Rotation, transform.localScale);
            }
        }
    }
}
