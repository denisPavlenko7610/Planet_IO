using System;
using System.Collections.Generic;

namespace UnityTemplates.Settings
{
	public sealed class MemorySettingsStore : ISettingsStore
	{
		private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

		public bool TryRead(string key, out string value)
		{
			ValidateKey(key);
			return _values.TryGetValue(key, out value);
		}

		public void Write(string key, string value)
		{
			ValidateKey(key);

			_values[key] = value ?? throw new ArgumentNullException(nameof(value));
		}

		public void Delete(string key)
		{
			ValidateKey(key);
			_values.Remove(key);
		}

		public void Flush() { }

		private static void ValidateKey(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentException("A settings key is required.", nameof(key));
			}
		}
	}
}
