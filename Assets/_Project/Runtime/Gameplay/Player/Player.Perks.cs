using System;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PlanetIO
{
    public sealed partial class Player
    {
        private const float OfferGraceSeconds = 0.75f;
        private const byte NoPerk = 255;

        private readonly NetworkVariable<ushort> _networkPerks = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly byte[] _offer = new byte[PerkRules.OfferSize];

        private PlanetGravity _gravity;
        private int _offerCount;
        private float _offerRemaining;
        private int _pendingOffers;
        private int _highestTier = TierRules.MinTier;

        public ushort PerkMask => IsSpawned ? _networkPerks.Value : (ushort)0;

        public float SpeedMultiplier =>
            PerkRules.SpeedMultiplier(PerkMask) * (1f + (Gravity != null ? Gravity.SlingshotBoost : 0f));

        public override int MoonSlotBonus => PerkRules.MoonSlotBonus(PerkMask);

        public override float GravityZoneMultiplier => PerkRules.GravityZoneMultiplier(PerkMask);

        internal override bool IsGravitySimulatedHere => IsOwner;

        protected override float MoonDamageMultiplier => PerkRules.MoonDamageMultiplier(PerkMask);

        protected override float AbsorbMultiplier => PerkRules.AbsorbMultiplier(PerkMask);

        protected override float CometHealFraction => PerkRules.CometHealFraction(PerkMask);

        private float BoostCost => _boostMassCost * PerkRules.BoostCostMultiplier(PerkMask);

        private PlanetGravity Gravity
        {
            get
            {
                if (_gravity == null)
                {
                    TryGetComponent(out _gravity);
                }

                return _gravity;
            }
        }

        public event Action<PerkId[], float> PerkOfferShown;

        public event Action PerkOfferClosed;

        public event Action<ushort> PerksChanged;

        public event Action Slingshot;

        public bool HasPerk(PerkId id) => PerkRules.Has(PerkMask, id);

        public bool TryConsumeAblativeCrust()
        {
            if (!IsServer || IsDefeated || !HasPerk(PerkId.AblativeCrust))
            {
                return false;
            }

            _networkPerks.Value = (ushort)(_networkPerks.Value & ~PerkRules.Bit(PerkId.AblativeCrust));
            _invincibilityTimeRemaining = _spawnInvincibilityTime;
            Capacity = Mathf.Max(InitialCapacity, Capacity * PerkRules.AblativeSurvivalFraction);
            return true;
        }

        public void ChoosePerk(int index)
        {
            if (index >= 0 && index < PerkRules.OfferSize)
            {
                ChoosePerkRpc((byte)index);
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void ChoosePerkRpc(byte index)
        {
            if (!IsServer || _offerCount == 0 || index >= _offerCount)
            {
                return;
            }

            ResolveOffer(index);
        }

        protected override void OnTierIncreased(int previousTier, int newTier)
        {
            int firstNewTier = Mathf.Max(_highestTier, previousTier) + 1;
            if (newTier < firstNewTier)
            {
                return;
            }

            _pendingOffers += newTier - firstNewTier + 1;
            _highestTier = newTier;
        }

        private void SubscribePerks()
        {
            _networkPerks.OnValueChanged += OnPerksValueChanged;
            if (Gravity != null)
            {
                Gravity.SlingshotTriggered += OnSlingshotTriggered;
            }
        }

        private void UnsubscribePerks()
        {
            _networkPerks.OnValueChanged -= OnPerksValueChanged;
            if (Gravity != null)
            {
                Gravity.SlingshotTriggered -= OnSlingshotTriggered;
            }
        }

        private void OnPerksValueChanged(ushort previous, ushort current)
        {
            PerksChanged?.Invoke(current);
        }

        private void OnSlingshotTriggered()
        {
            if (IsOwner)
            {
                Slingshot?.Invoke();
            }
        }

        private void UpdatePerkOffers(float deltaTime)
        {
            if (IsDefeated)
            {
                ClearOffers();
                return;
            }

            if (_offerCount > 0)
            {
                _offerRemaining -= deltaTime;
                if (_offerRemaining <= 0f)
                {
                    ResolveOffer(Random.Range(0, _offerCount));
                }

                return;
            }

            if (_pendingOffers > 0 && CanActInGameplay)
            {
                BeginOffer();
            }
        }

        private void BeginOffer()
        {
            _pendingOffers--;
            _offerCount = PerkRules.FillOffer(PerkMask, Random.Range(int.MinValue, int.MaxValue), _offer);
            if (_offerCount == 0)
            {
                return;
            }

            _offerRemaining = PerkRules.OfferSeconds + OfferGraceSeconds;
            ShowPerkOfferRpc(
                _offer[0],
                _offerCount > 1 ? _offer[1] : NoPerk,
                _offerCount > 2 ? _offer[2] : NoPerk,
                PerkRules.OfferSeconds);
        }

        private void ResolveOffer(int index)
        {
            PerkId chosen = (PerkId)_offer[index];
            _offerCount = 0;
            _networkPerks.Value = PerkRules.With(_networkPerks.Value, chosen);
            RefreshMoonSlots();
            ClosePerkOfferRpc();
        }

        private void ClearOffers()
        {
            _pendingOffers = 0;
            if (_offerCount > 0)
            {
                _offerCount = 0;
                ClosePerkOfferRpc();
            }
        }

        private void ResetPerksForNewLife()
        {
            ClearOffers();
            _highestTier = TierRules.MinTier;
            _networkPerks.Value = 0;
            ResetAbilitiesForNewLife();
            RefreshMoonSlots();
        }

        [Rpc(SendTo.Owner)]
        private void ShowPerkOfferRpc(byte first, byte second, byte third, float seconds)
        {
            var ids = new PerkId[PerkRules.OfferSize];
            int count = 0;
            foreach (byte value in new[] { first, second, third })
            {
                if (PerkRules.IsValid(value))
                {
                    ids[count++] = (PerkId)value;
                }
            }

            if (count == 0)
            {
                return;
            }

            Array.Resize(ref ids, count);
            PerkOfferShown?.Invoke(ids, seconds);
        }

        [Rpc(SendTo.Owner)]
        private void ClosePerkOfferRpc()
        {
            PerkOfferClosed?.Invoke();
        }
    }
}
