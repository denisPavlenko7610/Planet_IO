using System;

namespace UnityTemplates.Foundation
{
	internal static class Guard
	{
		public static T NotNull<T>(T value, string parameterName) where T : class
		{
			return value ?? throw new ArgumentNullException(parameterName);
		}

		public static double FiniteNonNegative(double value, string parameterName)
		{
			if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
			{
				throw new ArgumentOutOfRangeException(
					parameterName,
					value,
					"A finite, non-negative value is required."
				);
			}

			return value;
		}
	}
}
