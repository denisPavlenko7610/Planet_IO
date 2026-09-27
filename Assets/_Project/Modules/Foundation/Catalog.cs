using System;
using System.Collections.Generic;

namespace UnityTemplates.Foundation
{
	public static class Catalog
	{
		public static Dictionary<TKey, TItem> BuildUniqueDictionary<TKey, TItem>(
			IEnumerable<TItem> items,
			Func<TItem, TKey> keySelector,
			string parameterName,
			Func<TItem, bool> isValid = null,
			IEqualityComparer<TKey> comparer = null
		)
		{
			if (items == null)
			{
				throw new ArgumentNullException(parameterName);
			}

			if (keySelector == null)
			{
				throw new ArgumentNullException(nameof(keySelector));
			}

			Dictionary<TKey, TItem> dictionary = new(comparer ?? EqualityComparer<TKey>.Default);

			foreach (TItem item in items)
			{
				if (item == null)
				{
					throw new ArgumentException("The collection cannot contain null items.", parameterName);
				}

				if (isValid != null && !isValid(item))
				{
					throw new ArgumentException("The collection contains an invalid item.", parameterName);
				}

				TKey key = keySelector(item);
				if (!dictionary.TryAdd(key, item))
				{
					throw new ArgumentException($"Duplicate key '{key}'.", parameterName);
				}
			}

			return dictionary;
		}
	}
}
