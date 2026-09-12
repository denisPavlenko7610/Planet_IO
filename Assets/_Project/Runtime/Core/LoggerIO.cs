using UnityEngine;

namespace PlanetIO
{
    public static class LoggerIO
    {
        public static void Log(object message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(message);
#endif
        }

        public static void Log(object message, Object context)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(message, context);
#endif
        }

        public static void LogWarning(object message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning(message);
#endif
        }

        public static void LogWarning(object message, Object context)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning(message, context);
#endif
        }

        public static void LogError(object message)
        {
            Debug.LogError(message);
        }

        public static void LogError(object message, Object context)
        {
            Debug.LogError(message, context);
        }

        public static void LogException(System.Exception exception)
        {
            Debug.LogException(exception);
        }

        public static void LogException(System.Exception exception, Object context)
        {
            Debug.LogException(exception, context);
        }
    }
}
