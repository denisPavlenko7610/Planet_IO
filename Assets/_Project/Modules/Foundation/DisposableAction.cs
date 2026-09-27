using System;
using System.Threading;

namespace UnityTemplates.Foundation
{
	public sealed class DisposableAction : IDisposable
	{
		private Action _action;

		public DisposableAction(Action action)
		{
			_action = Guard.NotNull(action, nameof(action));
		}

		public bool IsDisposed => Volatile.Read(ref _action) == null;

		public void Dispose()
		{
			Interlocked.Exchange(ref _action, null)?.Invoke();
		}
	}
}
