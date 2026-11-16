using UnityEngine;

namespace PlanetIO
{
    public sealed class MoonLoadout
    {
        public const int MaxSlots = 8;

        private readonly byte[] _durability = new byte[MaxSlots];
        private readonly float[] _regrow = new float[MaxSlots];

        public int Slots { get; private set; }

        public byte AliveMask { get; private set; }

        public bool IsAlive(int index) =>
            index >= 0 && index < Slots && (AliveMask & (1 << index)) != 0;

        public bool SetSlots(int slots)
        {
            int clamped = Mathf.Clamp(slots, 0, MaxSlots);
            if (clamped == Slots)
            {
                return false;
            }

            byte previous = AliveMask;
            for (int index = 0; index < MaxSlots; index++)
            {
                if (index >= clamped)
                {
                    Clear(index);
                }
                else if (index >= Slots)
                {
                    Revive(index);
                }
            }

            Slots = clamped;
            return AliveMask != previous;
        }

        public bool Hit(int index)
        {
            if (!IsAlive(index))
            {
                return false;
            }

            _durability[index]--;
            if (_durability[index] > 0)
            {
                return false;
            }

            AliveMask = (byte)(AliveMask & ~(1 << index));
            _regrow[index] = MoonRules.RegrowSeconds;
            return true;
        }

        public bool Tick(float deltaTime)
        {
            bool changed = false;
            for (int index = 0; index < Slots; index++)
            {
                if (IsAlive(index) || _regrow[index] <= 0f)
                {
                    continue;
                }

                _regrow[index] -= deltaTime;
                if (_regrow[index] <= 0f)
                {
                    Revive(index);
                    changed = true;
                }
            }

            return changed;
        }

        public void CaptureMigration(PlanetMigrationState state)
        {
            state.MoonSlots = Slots;
            state.Durability = (byte[])_durability.Clone();
            state.Regrow = (float[])_regrow.Clone();
        }

        public void RestoreMigration(PlanetMigrationState state)
        {
            Reset();
            Slots = state.MoonSlots;
            for (int i = 0; i < MaxSlots; i++)
            {
                _durability[i] = state.Durability[i];
                _regrow[i] = state.Regrow[i];
                if (i < Slots && _durability[i] > 0) AliveMask |= (byte)(1 << i);
            }
        }

        public void Reset()
        {
            Slots = 0;
            AliveMask = 0;
            for (int index = 0; index < MaxSlots; index++)
            {
                _durability[index] = 0;
                _regrow[index] = 0f;
            }
        }

        private void Revive(int index)
        {
            _durability[index] = MoonRules.MaxDurability;
            _regrow[index] = 0f;
            AliveMask |= (byte)(1 << index);
        }

        private void Clear(int index)
        {
            _durability[index] = 0;
            _regrow[index] = 0f;
            AliveMask = (byte)(AliveMask & ~(1 << index));
        }
    }
}
