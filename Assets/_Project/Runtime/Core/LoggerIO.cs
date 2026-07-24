using UnityEngine;

namespace PlanetIO
{
    public static class LoggerIO
    {
        public static bool Enabled { get; set; } = Application.isEditor;

        public static void Log(object message)
        {
            if (!Enabled)
			{
				return;
			}

			Debug.Log(message);
        }

        public static void Log(object message, Object context)
        {
            if (!Enabled)
			{
				return;
			}

			Debug.Log(message, context);
        }

        public static void LogWarning(object message)
        {
            if (!Enabled)
			{
				return;
			}

			Debug.LogWarning(message);
        }

        public static void LogWarning(object message, Object context)
        {
            if (!Enabled)
			{
				return;
			}

			Debug.LogWarning(message, context);
        }

        public static void LogError(object message)
        {
            if (!Enabled)
			{
				return;
			}

			Debug.LogError(message);
        }

        public static void LogError(object message, Object context)
        {
            if (!Enabled)
			{
				return;
			}

			Debug.LogError(message, context);
        }

        public static void LogException(System.Exception exception)
        {
            if (!Enabled)
			{
				return;
			}

			Debug.LogException(exception);
        }

        public static void LogException(System.Exception exception, Object context)
        {
            if (!Enabled)
			{
				return;
			}

			Debug.LogException(exception, context);
        }
    }
}
