using System;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace PlanetIO
{
    public sealed partial class Player
    {
        private MatchMigrationStore _migration;
        private PlanetMigrationState _pendingMigration;
        private readonly NetworkVariable<bool> _restoredByMigration = new(false);
        public string MigrationIdentity { get; private set; }
        public bool WasMigrationRestored => _restoredByMigration.Value;
        public bool ContinueUsed => _continueUsedThisLife;
        [Inject] public void ConfigureMigration(MatchMigrationStore migration) => _migration = migration;

        public void PrepareMigration(string identity, PlanetMigrationState state)
        {
            MigrationIdentity = identity;
            _pendingMigration = state;
            if (state != null)
            {
                Capacity = state.Mass;
                transform.SetPositionAndRotation(state.Position, state.Rotation);
            }
        }

        public override PlanetMigrationState CaptureMigration()
        {
            var s = base.CaptureMigration();
            s.Identity = MigrationIdentity;
            s.Defeated = IsDefeated;
            s.ContinueUsed = _continueUsedThisLife;
            s.DefeatMass = _capacityAtDefeat;
            s.Protection = _invincibilityTimeRemaining;
            s.Boosting = _serverBoosting;
            s.BoostTimer = _boostTimer;
            s.Perks = PerkMask;
            s.HighestTier = _highestTier;
            s.OfferCount = _offerCount;
            s.PendingOffers = _pendingOffers;
            s.Offer = (byte[])_offer.Clone();
            s.OfferRemaining = _offerRemaining;
            s.PendingRestore = _pendingCapacityRestore;
            s.StealthRemaining = _stealthRemaining;
            s.StealthCooldown = _stealthCooldown;
            s.TrailPositions = (Vector2[])_trailPositions.Clone();
            s.TrailRemaining = (float[])_trailRemaining.Clone();
            s.TrailNext = _trailNext;
            s.TrailSpawnTimer = _trailSpawnTimer;
            s.TrailStrikeTimer = _trailStrikeTimer;
            s.Color = _networkColor.Value;
            s.Skin = _networkSkin.Value;
            s.LootIds = (_lootDrop as IMigrationLootDrop)?.CaptureFoodIds() ?? Array.Empty<ulong>();
            GetComponent<PlayerMovement>()?.CaptureMigration(s);
            return s;
        }

        private void ApplyPendingMigration()
        {
            if (!IsServer || _pendingMigration == null)
            {
                return;
            }

            var s = _pendingMigration;
            _restoredByMigration.Value = true;
            _highestTier = s.HighestTier;
            _networkPerks.Value = s.Perks;
            RestorePlanetMigration(s);
            _networkColor.Value = s.Color;
            _networkSkin.Value = s.Skin;
            _continueUsedThisLife = s.ContinueUsed;
            _capacityAtDefeat = s.DefeatMass;
            _invincibilityTimeRemaining = s.Protection;
            _networkSpawnProtected.Value = s.Protection > 0;
            _serverBoosting = s.Boosting;
            _networkBoosting.Value = s.Boosting;
            _boostTimer = s.BoostTimer;
            _offerCount = s.OfferCount;
            _pendingOffers = s.PendingOffers;
            _offerRemaining = s.OfferRemaining;
            if (s.Offer != null)
            {
                Array.Copy(s.Offer, _offer, Math.Min(_offer.Length, s.Offer.Length));
            }
            _pendingCapacityRestore = s.PendingRestore;
            _stealthRemaining = s.StealthRemaining;
            _stealthCooldown = s.StealthCooldown;
            _networkStealth.Value = s.StealthRemaining > 0;
            if (s.TrailPositions != null)
            {
                Array.Copy(s.TrailPositions, _trailPositions, Math.Min(TrailCapacity, s.TrailPositions.Length));
            }
            if (s.TrailRemaining != null)
            {
                Array.Copy(s.TrailRemaining, _trailRemaining, Math.Min(TrailCapacity, s.TrailRemaining.Length));
            }
            _trailNext = s.TrailNext;
            _trailSpawnTimer = s.TrailSpawnTimer;
            _trailStrikeTimer = s.TrailStrikeTimer;
            _networkDefeated.Value = s.Defeated;
            SetBodyActive(!s.Defeated);
            _lootDrop = (_lootSpawnService as IMigrationLootFactory)?.RestoreLoot(
                s.LootIds ?? Array.Empty<ulong>(),
                _migration.ObjectIds
            );
            GetComponent<PlayerMovement>()?.RestoreMigration(s);
            if (_migration != null)
            {
                _migration.BindObject(s.ObjectId, NetworkObjectId, this);
            }
            if (_migration != null)
            {
                foreach (var foodState in _migration.RestoredFood)
                {
                    if (_migration.ObjectIds.TryGetValue(foodState.ObjectId, out ulong foodId) &&
                        NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(foodId, out var obj) &&
                        obj.TryGetComponent(out Food food))
                    {
                        food.RestoreDropper(foodState, _migration.ObjectIds);
                    }
                }
            }
            _pendingMigration = null;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void RequestRestoredOfferRpc()
        {
            if (_restoredByMigration.Value && _offerCount > 0)
            {
                ShowPerkOfferRpc(
                    _offer[0],
                    _offerCount > 1 ? _offer[1] : NoPerk,
                    _offerCount > 2 ? _offer[2] : NoPerk,
                    _offerRemaining
                );
            }
        }
    }
}
