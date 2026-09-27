using System;
using System.Threading;
using UnityEngine;

namespace UnityTemplates.Foundation
{
	public sealed class AsyncGate
	{
		private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

		public async Awaitable<Lease> EnterAsync(CancellationToken cancellationToken = default)
		{
			await _semaphore.WaitAsync(cancellationToken);
			return new Lease(_semaphore);
		}

		public sealed class Lease : IDisposable
		{
			private SemaphoreSlim _semaphore;

			internal Lease(SemaphoreSlim semaphore)
			{
				_semaphore = semaphore;
			}

			public bool IsReleased => Volatile.Read(ref _semaphore) == null;

			public void Dispose()
			{
				Interlocked.Exchange(ref _semaphore, null)?.Release();
			}
		}
	}
}
