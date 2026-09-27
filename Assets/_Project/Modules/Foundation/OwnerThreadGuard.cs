using System;
using System.Threading;

namespace UnityTemplates.Foundation
{
	public sealed class OwnerThreadGuard
	{
		private readonly int _ownerThreadId = Thread.CurrentThread.ManagedThreadId;

		public void EnsureOwnerThread()
		{
			if (Thread.CurrentThread.ManagedThreadId != _ownerThreadId)
			{
				throw new InvalidOperationException(
					$"This instance must be used from the thread that created it (managed thread {_ownerThreadId})."
				);
			}
		}
	}
}
