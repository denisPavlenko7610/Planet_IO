using System.Collections.Generic;

namespace PlanetIO
{
    public sealed class ExpiryQueue<T>
    {
        private struct Entry
        {
            public float ExpiresAt;
            public long Sequence;
            public int Version;
            public T Item;
        }

        private readonly List<Entry> _heap = new();
        private long _sequence;

        public int Count => _heap.Count;

        public void Enqueue(T item, int version, float expiresAt)
        {
            _heap.Add(new Entry { ExpiresAt = expiresAt, Sequence = _sequence++, Version = version, Item = item });
            SiftUp(_heap.Count - 1);
        }

        public bool TryDequeueExpired(float now, out T item, out int version)
        {
            if (_heap.Count == 0 || _heap[0].ExpiresAt > now)
            {
                item = default;
                version = 0;
                return false;
            }

            Entry top = _heap[0];
            int last = _heap.Count - 1;
            _heap[0] = _heap[last];
            _heap.RemoveAt(last);
            if (last > 0)
            {
                SiftDown(0);
            }

            item = top.Item;
            version = top.Version;
            return true;
        }

        public void Clear()
        {
            _heap.Clear();
        }

        private static bool Earlier(Entry left, Entry right)
        {
            return left.ExpiresAt < right.ExpiresAt ||
                   left.ExpiresAt == right.ExpiresAt && left.Sequence < right.Sequence;
        }

        private void SiftUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (!Earlier(_heap[index], _heap[parent]))
                {
                    return;
                }

                (_heap[index], _heap[parent]) = (_heap[parent], _heap[index]);
                index = parent;
            }
        }

        private void SiftDown(int index)
        {
            int count = _heap.Count;
            while (true)
            {
                int left = index * 2 + 1;
                int right = left + 1;
                int smallest = index;
                if (left < count && Earlier(_heap[left], _heap[smallest]))
                {
                    smallest = left;
                }

                if (right < count && Earlier(_heap[right], _heap[smallest]))
                {
                    smallest = right;
                }

                if (smallest == index)
                {
                    return;
                }

                (_heap[index], _heap[smallest]) = (_heap[smallest], _heap[index]);
                index = smallest;
            }
        }
    }
}
