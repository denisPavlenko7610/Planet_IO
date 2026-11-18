using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace PlanetIO
{
    public abstract partial class PlanetScale
    {
        private readonly NetworkVariable<bool> _networkLeader = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public bool IsLeader => IsSpawned && _networkLeader.Value;

        public virtual bool IsHiddenFromRivals => MapFeatureField.IsInNebula(transform.position);

        internal virtual bool IsGravitySimulatedHere => IsServer;

        public virtual float GravityZoneMultiplier => 1f;

        public float GravityZoneRadius => GravityRules.ZoneRadius(BodyRadius, GravityZoneMultiplier);

        protected virtual float AbsorbMultiplier => 1f;

        protected virtual float CometHealFraction => 0f;

        protected void AbsorbVictim(PlanetScale victim)
        {
            if (victim == null)
            {
                return;
            }

            float bounty = victim.IsLeader ? CombatRules.LeaderBountyMultiplier : 1f;
            float gained = CombatRules.AbsorbedMass(victim.Capacity, _killAbsorbFraction);
            Grow(gained * AbsorbMultiplier * bounty);
        }

        public bool Pulls(PlanetScale body)
        {
            return body != null && body != this && GravityRules.Affects(Capacity, body.Capacity);
        }

        public virtual void Teleport(Vector2 position)
        {
            TeleportLocal(position);
        }

        protected void TeleportLocal(Vector2 position)
        {
            if (TryGetComponent(out Rigidbody2D body))
            {
                body.linearVelocity = Vector2.zero;
                body.position = position;
            }

            if (TryGetComponent(out NetworkTransform networkTransform))
            {
                networkTransform.Teleport(position, transform.rotation, transform.localScale);
            }
            else
            {
                transform.position = position;
            }
        }

        public bool TryApplyHazard(float massFraction, out float dealt)
        {
            dealt = 0f;
            if (!IsServer || !IsSpawned || !IsCombatActive || IsHarmImmune)
            {
                return false;
            }

            float before = Capacity;
            TakeDamage(before * massFraction * IncomingDamageMultiplier);
            dealt = Mathf.Max(0f, before - Capacity);
            return true;
        }

        private void RefreshLeaderServer()
        {
            bool leader = AreMoonsVisible && PlanetDirectory.ResolveLeaderId() == NetworkObjectId;
            if (_networkLeader.Value != leader)
            {
                _networkLeader.Value = leader;
            }
        }
    }
}
