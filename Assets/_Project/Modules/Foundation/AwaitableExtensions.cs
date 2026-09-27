using UnityEngine;

namespace UnityTemplates.Foundation
{
	public static class AwaitableExtensions
	{
		public static Awaitable Completed => GetCompletedAwaitable();

		public static Awaitable GetCompletedAwaitable()
		{
			AwaitableCompletionSource source = new AwaitableCompletionSource();
			Awaitable awaitable = source.Awaitable;
			source.SetResult();
			return awaitable;
		}

		public static Awaitable<T> FromResult<T>(T result)
		{
			AwaitableCompletionSource<T> source = new AwaitableCompletionSource<T>();
			Awaitable<T> awaitable = source.Awaitable;
			source.SetResult(result);
			return awaitable;
		}
	}
}
