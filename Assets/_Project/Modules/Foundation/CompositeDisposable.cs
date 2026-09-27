using System;
using System.Collections.Generic;

namespace UnityTemplates.Foundation
{
	public sealed class CompositeDisposable : IDisposable
	{
		private readonly object _gate = new object();
		private readonly List<IDisposable> _items;
		private bool _isDisposed;

		public CompositeDisposable()
		{
			_items = new List<IDisposable>();
		}

		public CompositeDisposable(IEnumerable<IDisposable> items) : this()
		{
			Guard.NotNull(items, nameof(items));
			foreach (IDisposable item in items)
			{
				Guard.NotNull(item, nameof(items));
				_items.Add(item);
			}
		}

		public bool IsDisposed
		{
			get
			{
				lock (_gate)
				{
					return _isDisposed;
				}
			}
		}

		public int Count
		{
			get
			{
				lock (_gate)
				{
					return _items.Count;
				}
			}
		}

		public bool Add(IDisposable item)
		{
			Guard.NotNull(item, nameof(item));

			lock (_gate)
			{
				if (!_isDisposed)
				{
					_items.Add(item);
					return true;
				}
			}

			item.Dispose();
			return false;
		}

		public bool Remove(IDisposable item)
		{
			if (item == null)
			{
				return false;
			}

			lock (_gate)
			{
				return !_isDisposed && _items.Remove(item);
			}
		}

		public void Clear()
		{
			IDisposable[] items;
			lock (_gate)
			{
				if (_isDisposed || _items.Count == 0)
				{
					return;
				}

				items = _items.ToArray();
				_items.Clear();
			}

			DisposeAll(items);
		}

		public void Dispose()
		{
			IDisposable[] items;
			lock (_gate)
			{
				if (_isDisposed)
				{
					return;
				}

				_isDisposed = true;
				items = _items.ToArray();
				_items.Clear();
			}

			DisposeAll(items);
		}

		private static void DisposeAll(IReadOnlyList<IDisposable> items)
		{
			List<Exception> exceptions = null;
			for (int index = items.Count - 1; index >= 0; index--)
			{
				try
				{
					items[index].Dispose();
				}
				catch (Exception exception)
				{
					exceptions ??= new List<Exception>();
					exceptions.Add(exception);
				}
			}

			if (exceptions != null)
			{
				throw new AggregateException("One or more disposables failed to dispose.", exceptions);
			}
		}
	}
}
