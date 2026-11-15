using Unity.Netcode;
using UnityEngine;

namespace PlanetIO
{
    public sealed partial class Player
    {
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void SetBoostRpc(bool boosting)
        {
            SetServerBoosting(CanActInGameplay && boosting && CanBoost);
        }

        private void SetServerBoosting(bool boosting)
        {
            bool wasBoosting = _serverBoosting;
            _serverBoosting = boosting;
            _networkBoosting.Value = boosting;

            if (wasBoosting && !boosting)
            {
                OnBoostEnded();
            }
        }

        private void ResetBoost()
        {
            SetServerBoosting(false);
            _boostTimer = 0f;
        }

        private void UpdateBoost(float deltaTime)
        {
            if (!_serverBoosting || !CanActInGameplay || !CanBoost)
            {
                _boostTimer = 0f;
                if (_serverBoosting)
                {
                    SetServerBoosting(false);
                }

                return;
            }

            _boostTimer -= deltaTime;
            if (_boostTimer > 0f)
            {
                return;
            }

            _boostTimer = _boostMassConsumptionInterval;

            if (!Shrink(BoostCost))
            {
                SetServerBoosting(false);
                return;
            }

            CreateFoodBehindPlayer();
            if (!CanBoost)
            {
                SetServerBoosting(false);
            }
        }

        private void CreateFoodBehindPlayer()
        {
            if (_pointSpawnTransform == null)
            {
                return;
            }

            Vector2 spawnPosition = _pointSpawnTransform.position;
            if (_rigidbody2D != null)
            {
                Vector2 velocity = _rigidbody2D.linearVelocity;
                if (velocity.sqrMagnitude > 0.001f)
                {
                    spawnPosition = (Vector2)transform.position - velocity.normalized * Capacity * 0.3f;
                    spawnPosition = Constants.ClampToWorld(spawnPosition);
                    _pointSpawnTransform.position = spawnPosition;
                }
            }

            _foodSpawnService.SpawnAt(_pointSpawnTransform, BoostCost * BoostDropNutritionFraction);
        }

        private void OnBoostingChanged(bool _, bool boosting)
        {
            _visualEffects?.SetBoosting(boosting);
        }
    }
}
