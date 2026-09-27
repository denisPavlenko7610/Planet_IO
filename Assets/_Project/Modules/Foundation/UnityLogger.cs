using System;
using System.Diagnostics;
using UnityEngine;

namespace UnityTemplates.Foundation
{
	/// <summary>
	/// Shared wrapper around UnityEngine.Debug.
	/// All logging in the runtime codebase goes through this class so it can be
	/// disabled at runtime (IsEnabled) and stripped from release players
	/// ([Conditional] UNITY_EDITOR / DEVELOPMENT_BUILD).
	/// </summary>
	public static class UnityLogger
	{
		public static bool IsEnabled { get; set; } = true;

		[Conditional("UNITY_EDITOR")]
		[Conditional("DEVELOPMENT_BUILD")]
		public static void Log(string message, UnityEngine.Object context = null)
		{
			if (!IsEnabled)
			{
				return;
			}

			UnityEngine.Debug.Log(message, context);
		}

		[Conditional("UNITY_EDITOR")]
		[Conditional("DEVELOPMENT_BUILD")]
		public static void LogWarning(string message, UnityEngine.Object context = null)
		{
			if (!IsEnabled)
			{
				return;
			}

			UnityEngine.Debug.LogWarning(message, context);
		}

		[Conditional("UNITY_EDITOR")]
		[Conditional("DEVELOPMENT_BUILD")]
		public static void LogError(string message, UnityEngine.Object context = null)
		{
			if (!IsEnabled)
			{
				return;
			}

			UnityEngine.Debug.LogError(message, context);
		}

		[Conditional("UNITY_EDITOR")]
		[Conditional("DEVELOPMENT_BUILD")]
		public static void LogException(Exception exception, UnityEngine.Object context = null)
		{
			if (!IsEnabled)
			{
				return;
			}

			UnityEngine.Debug.LogException(exception, context);
		}
	}
}
