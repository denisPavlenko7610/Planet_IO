using UnityEngine;

namespace PlanetIO
{
    public abstract partial class PlanetScale
    {
        private const int BodyFoodBufferSize = 32;

        private readonly Collider2D[] _bodyFoodBuffer = new Collider2D[BodyFoodBufferSize];
        private ContactFilter2D _bodyFoodFilter;
        private bool _bodyFoodFilterReady;
        private float _bodyFoodSweepTimer;

        private void SweepFoodInsideBody(float deltaTime)
        {
            if (!FoodSweepRules.Advance(ref _bodyFoodSweepTimer, deltaTime))
            {
                return;
            }

            if (!_bodyFoodFilterReady)
            {
                _bodyFoodFilter = ContactFilter2D.noFilter;
                _bodyFoodFilter.SetLayerMask(LayerMask.GetMask(Constants.FoodLayerName));
                _bodyFoodFilterReady = true;
            }

            int hitCount = Physics2D.OverlapCircle(transform.position, BodyRadius, _bodyFoodFilter, _bodyFoodBuffer);
            for (int index = 0; index < hitCount; index++)
            {
                Collider2D hit = _bodyFoodBuffer[index];
                if (hit == null ||
                    !hit.TryGetComponent(out Food food) ||
                    food.IsMagnetImmuneFor(NetworkObjectId))
                {
                    continue;
                }

                HandleEntityCollision(hit);
            }
        }
    }
}
