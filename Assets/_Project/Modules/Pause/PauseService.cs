using System;
using System.Collections.Generic;

namespace UnityTemplates.Pause
{
	public readonly struct PauseChanged
	{
		public PauseChanged(
			bool isPaused,
			IReadOnlyList<string> activeReasons
		)
		{
			IsPaused = isPaused;
			ActiveReasons = activeReasons;
		}

		public bool IsPaused { get; }

		public IReadOnlyList<string> ActiveReasons { get; }
	}

	public sealed class PauseService : IPauseService, IDisposable
	{
		private readonly Dictionary<string, int> _reasonCounts = new(StringComparer.Ordinal);

		private IReadOnlyList<string> _reasons = Array.Empty<string>();

		private bool _disposed;

		public bool IsPaused => _reasonCounts.Count > 0;

		public IReadOnlyList<string> ActiveReasons => _reasons;

		public event Action<PauseChanged> Changed;

		public IDisposable RequestPause(string reason)
		{
			ThrowIfDisposed();

			if (string.IsNullOrWhiteSpace(reason))
			{
				throw new ArgumentException("Pause reason cannot be empty.", nameof(reason));
			}

			if (_reasonCounts.TryGetValue(reason, out int count))
			{
				_reasonCounts[reason] = count + 1;
			}
			else
			{
				_reasonCounts.Add(reason, 1);
				Publish();
			}

			return new PauseHandle(this, reason);
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			bool wasPaused = IsPaused;

			_reasonCounts.Clear();
			_reasons = Array.Empty<string>();

			Action<PauseChanged> changed = Changed;
			Changed = null;

			if (wasPaused)
			{
				changed?.Invoke(new PauseChanged(false, _reasons));
			}
		}

		private void Release(string reason)
		{
			if (_disposed)
			{
				return;
			}

			if (!_reasonCounts.TryGetValue(reason, out int count))
			{
				return;
			}

			if (count > 1)
			{
				_reasonCounts[reason] = count - 1;
				return;
			}

			_reasonCounts.Remove(reason);

			Publish();
		}

		private void Publish()
		{
			string[] reasons = new string[_reasonCounts.Count];
			_reasonCounts.Keys.CopyTo(reasons, 0);

			Array.Sort(reasons, StringComparer.Ordinal);

			_reasons = Array.AsReadOnly(reasons);

			Changed?.Invoke(new PauseChanged(IsPaused, _reasons));
		}

		private void ThrowIfDisposed()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(nameof(PauseService));
			}
		}

		private sealed class PauseHandle : IDisposable
		{
			private PauseService _owner;
			private readonly string _reason;

			public PauseHandle(PauseService owner, string reason)
			{
				_owner = owner;
				_reason = reason;
			}

			public void Dispose()
			{
				PauseService owner = _owner;
				_owner = null;

				owner?.Release(_reason);
			}
		}
	}
}
