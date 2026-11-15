using Unity.Netcode;
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

        public virtual bool IsHiddenFromRivals => false;

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
