using System;
using System.Threading;
using UnityEngine;

namespace UnityTemplates.Foundation
{
	public static class AsyncHelper
	{
		public static async void FireAndForget(
			Func<CancellationToken, Awaitable> action,
			CancellationToken cancellationToken,
			UnityEngine.Object context = null
		)
		{
			try
			{
				await action(cancellationToken);
			}
			catch (OperationCanceledException) { }
			catch (Exception exception)
			{
				UnityLogger.LogException(exception, context);
			}
		}
	}
}
