using System;
using System.Collections.Generic;

namespace UnityTemplates.Foundation
{
	public static class SafeEvent
	{
		public static void Raise<T>(Action<T> handlers, T argument)
		{
			Action<T> snapshot = handlers;
			if (snapshot == null)
			{
				return;
			}

			List<Exception> exceptions = null;
			foreach (Action<T> handler in snapshot.GetInvocationList())
			{
				try
				{
					handler(argument);
				}
				catch (Exception exception)
				{
					UnityLogger.LogException(exception);
					(exceptions ??= new List<Exception>()).Add(exception);
				}
			}

			RethrowInDevelopmentBuild(exceptions);
		}

		public static void Raise(Action handlers)
		{
			Action snapshot = handlers;
			if (snapshot == null)
			{
				return;
			}

			List<Exception> exceptions = null;
			foreach (Action handler in snapshot.GetInvocationList())
			{
				try
				{
					handler();
				}
				catch (Exception exception)
				{
					UnityLogger.LogException(exception);
					(exceptions ??= new List<Exception>()).Add(exception);
				}
			}

			RethrowInDevelopmentBuild(exceptions);
		}

		public static void Raise<TEventArgs>(EventHandler<TEventArgs> handlers, object sender, TEventArgs args)
			where TEventArgs : EventArgs
		{
			EventHandler<TEventArgs> snapshot = handlers;
			if (snapshot == null)
			{
				return;
			}

			List<Exception> exceptions = null;
			foreach (EventHandler<TEventArgs> handler in snapshot.GetInvocationList())
			{
				try
				{
					handler(sender, args);
				}
				catch (Exception exception)
				{
					UnityLogger.LogException(exception);
					(exceptions ??= new List<Exception>()).Add(exception);
				}
			}

			RethrowInDevelopmentBuild(exceptions);
		}

		private static void RethrowInDevelopmentBuild(List<Exception> exceptions)
		{
			#if UNITY_EDITOR || DEVELOPMENT_BUILD
			if (exceptions != null)
			{
				throw new AggregateException(
					"One or more event subscribers threw while handling an event. See the logged inner exceptions.",
					exceptions
				);
			}
			#endif
		}
	}
}
