namespace PlanetIO
{
	public static class MovementRules
	{
		public const float PlayerSpeed = 3.6f;
		public const float PlayerBoostSpeed = 7.2f;
		public const float TurnSpeed = 360f;
		public const float TurnSmoothTime = 0.18f;

		public static float Turn(float current, float target, ref float velocity, float smoothTime, float maxSpeed, float deltaTime) =>
			UnityEngine.Mathf.SmoothDampAngle(current, target, ref velocity, smoothTime, maxSpeed, deltaTime);

		public const float BotSpeed = 3f;
	}
}
