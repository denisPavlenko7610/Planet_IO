using UnityEngine;

namespace PlanetIO
{
    public static class StickResponse
    {
        public static Vector2 ApplyDeadZone(Vector2 input, float deadZone)
        {
            float magnitude = input.magnitude;
            if (magnitude <= deadZone)
            {
                return Vector2.zero;
            }

            float scaled = Mathf.Min(1f, (magnitude - deadZone) / (1f - deadZone));
            return input / magnitude * scaled;
        }

        public static Vector2 PreCompensate(Vector2 shaped, float processorMin, float processorMax)
        {
            float magnitude = shaped.magnitude;
            if (magnitude <= 0f)
            {
                return Vector2.zero;
            }

            return shaped / magnitude * Mathf.Lerp(processorMin, processorMax, magnitude);
        }
    }
}
