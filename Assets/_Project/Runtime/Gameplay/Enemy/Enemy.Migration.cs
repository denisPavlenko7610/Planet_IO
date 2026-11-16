using Unity.Netcode;
using UnityEngine;

namespace PlanetIO
{
    public sealed partial class Enemy
    {
        private readonly NetworkVariable<ulong> _appearanceSeed = new();
        public ulong AppearanceSeed => _appearanceSeed.Value;

        public override PlanetMigrationState CaptureMigration()
        {
            var s = base.CaptureMigration();
            s.AppearanceSeed = AppearanceSeed;
            GetComponent<EnemyMovement>().CaptureMigration(s);
            return s;
        }

        public void RestoreMigration(PlanetMigrationState s)
        {
            RestorePlanetMigration(s);
            _appearanceSeed.Value = s.AppearanceSeed;
            SetDeterministicSprite();
            ApplyDeterministicTint();
            GetComponent<EnemyMovement>().RestoreMigration(s);
        }

        private void AppearanceChanged(ulong previous, ulong current)
        {
            SetDeterministicSprite();
            ApplyDeterministicTint();
        }
    }
}
