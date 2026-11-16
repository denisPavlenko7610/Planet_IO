using UnityEngine;

namespace PlanetIO
{
    public static class CameraZoomRules
    {
        public const float GrowthExponent = 0.15f;
        public const float GrowthHoldSeconds = 0.35f;

        public static float MoonView(float outerExtent, float margin, float aspect, float lookAhead)
        {
            float offset = Mathf.Clamp(lookAhead, 0f, 0.5f);
            float horizontal = outerExtent * margin / Mathf.Max(0.1f, aspect - offset);
            float vertical = outerExtent * margin / (1f - offset);
            return Mathf.Max(horizontal, vertical);
        }

        public static float Ratio(float capacity, float exponent)
        {
            float growth = Mathf.Max(1f, capacity / PlanetSizeRules.ReferenceCapacity);
            return Mathf.Pow(growth, Mathf.Max(0f, exponent));
        }
    }

    public sealed class GrowthZoomState
    {
        private float _holdRemaining;
        private bool _holding;
        private bool _following;

        public float Target { get; private set; }

        public void Reset(float target)
        {
            Target = target;
            _holdRemaining = 0f;
            _holding = false;
            _following = false;
        }

        public float Step(float desired, float requiredView, float deltaTime)
        {
            if (desired <= Target)
            {
                Reset(desired);
            }
            else
            {
                if (!_holding && !_following)
                {
                    _holdRemaining = CameraZoomRules.GrowthHoldSeconds;
                    _holding = true;
                }

                _holdRemaining -= Mathf.Max(0f, deltaTime);
                if (_holdRemaining <= 0f)
                {
                    Target = desired;
                    _holding = false;
                    _following = true;
                }
            }

            Target = Mathf.Max(Target, requiredView);
            return Target;
        }
    }
}
