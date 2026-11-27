using UnityEngine;

namespace PlanetIO
{
	public sealed class FoodRecord : ICapacity
	{
		internal FoodWorld World;
		internal int Slot;
		internal Vector2 StoredPosition;
		internal float StoredCapacity;
		internal bool Claimed;
		internal int GridCell = -1;
		internal int Next = -1;
		internal int Previous = -1;

		public ulong Id { get; internal set; }
		public uint Generation { get; internal set; }
		public bool Active { get; internal set; }
		public bool IsDropped { get; internal set; }
		public bool IsCometTrail { get; internal set; }
		public byte Flavor { get; internal set; }
		public int DensityCell { get; internal set; } = -1;
		public float Nutrition { get; internal set; }
		public float ValueMultiplier { get; internal set; } = 1f;
		public float ExpiresAt { get; internal set; }
		public ulong DropperId { get; internal set; } = ulong.MaxValue;
		public float ImmuneUntil { get; internal set; }
		public int LifecycleVersion => (int)Generation;
		public float Radius => Capacity * 0.5f;

		public Vector2 Position
		{
			get => StoredPosition;
			set => World.Move(this, value);
		}

		public float Capacity
		{
			get => StoredCapacity;
			set { World.RequireAuthority(); StoredCapacity = Mathf.Max(0.001f, value); World.Touch(this); }
		}

		public float GetGrowth(float multiplier) => GrowthRules.FoodGrowth(Capacity, multiplier, ValueMultiplier, Nutrition);
		public bool IsMagnetImmuneFor(ulong id) => DropperId == id && World.Clock < ImmuneUntil;
		public bool TryClaim()
		{
			World.RequireAuthority();
			if (!Active || Claimed) return false;
			Claimed = true;
			return true;
		}

		public void ResetClaim() { World.RequireAuthority(); Claimed = false; }
		public void SetNutrition(float value) { World.RequireAuthority(); Nutrition = Mathf.Max(0f, value); World.Touch(this); }
		public void SetValueMultiplier(float value) { World.RequireAuthority(); ValueMultiplier = Mathf.Max(1f, value); World.Touch(this); }
		public void SetDropper(ulong id, float immunity)
		{
			World.RequireAuthority();
			DropperId = id;
			ImmuneUntil = World.Clock + Mathf.Max(0f, immunity);
			World.Touch(this);
		}
	}
}
