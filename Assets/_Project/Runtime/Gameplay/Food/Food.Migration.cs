using System.Collections.Generic;
using UnityEngine;

namespace PlanetIO
{
    public sealed partial class Food
    {
        public float DropExpiresAt { get; set; }

        public FoodMigrationState CaptureMigration()
        {
            return new FoodMigrationState
            {
                ObjectId = NetworkObjectId, Position = transform.position, Scale = Capacity,
                Nutrition = Nutrition, Value = ValueMultiplier, Dropped = IsDropped, Cell = DensityCell,
                DropperId = _dropperObjectId, Immunity = Mathf.Max(0f, _magnetImmuneUntil - Time.time),
                Remaining = 0f
            };
        }

        public void RestoreMigration(FoodMigrationState s)
        {
            ResetClaim();
            if (s.Dropped)
            {
                MarkAsDropped();
            }
            else
            {
                MarkAsStored();
            }
            SetNutrition(s.Nutrition);
            SetValueMultiplier(s.Value);
            DensityCell = s.Cell;
        }

        public void RestoreDropper(FoodMigrationState s, IReadOnlyDictionary<ulong, ulong> ids)
        {
            if (ids.TryGetValue(s.DropperId, out ulong id))
            {
                SetDropper(id, s.Immunity);
            }
        }
    }
}
