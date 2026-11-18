using System.Text;
using UnityEngine;

namespace PlanetIO.Infrastructure.Analytics
{
	public sealed class LocalAnalyticsService : IAnalyticsService
	{
		public void Track(string eventName, params (string Key, object Value)[] parameters)
		{
			if (string.IsNullOrEmpty(eventName) || !Debug.isDebugBuild)
			{
				return;
			}

			Debug.Log(Format(eventName, parameters ?? System.Array.Empty<(string, object)>()));
		}

		private static string Format(string eventName, (string Key, object Value)[] parameters)
		{
			var builder = new StringBuilder("[Analytics] ").Append(eventName);
			foreach ((string key, object value) in parameters)
			{
				builder.Append(' ').Append(key).Append('=').Append(value);
			}

			return builder.ToString();
		}
	}
}
