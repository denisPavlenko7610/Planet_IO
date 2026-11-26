using UnityEngine;

namespace PlanetIO
{
    public sealed class SlingshotState
    {
        public const float TriggerSeconds = 0.5f;
        public const float BoostFraction = 0.2f;
        public const float BoostSeconds = 2f;

        private float _tangentTime;
        private float _boostRemaining;

        public float Boost => _boostRemaining > 0f ? BoostFraction * (_boostRemaining / BoostSeconds) : 0f;

        public bool Tick(float deltaTime, bool tangentialInZone)
        {
            bool wasActive = _boostRemaining > 0f;
            if (wasActive)
            {
                _boostRemaining = Mathf.Max(0f, _boostRemaining - deltaTime);
            }

            if (!tangentialInZone)
            {
                _tangentTime = Mathf.Max(0f, _tangentTime - deltaTime);
                return false;
            }

            _tangentTime += deltaTime;
            if (_tangentTime < TriggerSeconds)
            {
                return false;
            }

            _tangentTime = 0f;
            _boostRemaining = BoostSeconds;
            return !wasActive;
        }

        public void Reset()
        {
            _tangentTime = 0f;
            _boostRemaining = 0f;
        }
    }
}
