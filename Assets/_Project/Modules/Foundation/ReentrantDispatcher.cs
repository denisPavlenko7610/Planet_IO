using System;
using System.Collections.Generic;

namespace UnityTemplates.Foundation
{
	public sealed class ReentrantDispatcher<T>
	{
		private readonly Queue<T> _queue = new Queue<T>();
		private readonly Action<T> _handler;
		private bool _dispatching;

		public ReentrantDispatcher(Action<T> handler)
		{
			_handler = handler ?? throw new ArgumentNullException(nameof(handler));
		}

		public void Dispatch(T item)
		{
			_queue.Enqueue(item);
			if (_dispatching)
			{
				return;
			}

			_dispatching = true;
			try
			{
				while (_queue.Count != 0)
				{
					_handler(_queue.Dequeue());
				}
			}
			finally
			{
				_dispatching = false;
			}
		}
	}
}
