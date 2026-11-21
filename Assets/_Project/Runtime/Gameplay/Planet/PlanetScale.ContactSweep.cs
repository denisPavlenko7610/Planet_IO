using System.Collections.Generic;
using UnityEngine;

namespace PlanetIO
{
	public abstract partial class PlanetScale
	{
		private float _contactSweepTimer;

		protected virtual void ResolvePlanetContact(PlanetScale other)
		{
		}

		private void SweepPlanetContacts(float deltaTime)
		{
			if (!FoodSweepRules.Advance(ref _contactSweepTimer, deltaTime))
			{
				return;
			}

			Vector2 center = transform.position;
			float radius = BodyRadius;
			IReadOnlyList<PlanetScale> planets = PlanetDirectory.All;

			for (int index = 0; index < planets.Count; index++)
			{
				PlanetScale other = planets[index];

				if (other == null || other == this || !other.IsSpawned || !other.isActiveAndEnabled || !CanDevour(other))
				{
					continue;
				}

				if (!CombatRules.InContact(center, radius, other.transform.position, other.BodyRadius))
				{
					continue;
				}

				ResolvePlanetContact(other);

				if (!IsCombatActive)
				{
					return;
				}
			}
		}
	}
}
