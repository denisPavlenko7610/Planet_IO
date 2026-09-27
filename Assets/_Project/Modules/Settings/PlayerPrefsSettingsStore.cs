using System;
using UnityEngine;

namespace UnityTemplates.Settings
{
	public sealed class PlayerPrefsSettingsStore : ISettingsStore
	{
		private const string DefaultPrefix = "settings.";

		private readonly string _prefix;

		public PlayerPrefsSettingsStore(string prefix = DefaultPrefix)
		{
			_prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
		}

		public bool TryRead(string key, out string value)
		{
			string storageKey = Resolve(key);

			if (!PlayerPrefs.HasKey(storageKey))
			{
				value = null;
				return false;
			}

			value = PlayerPrefs.GetString(storageKey);

			return true;
		}

		public void Write(string key, string value)
		{
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			PlayerPrefs.SetString(Resolve(key), value);
		}

		public void Delete(string key)
		{
			PlayerPrefs.DeleteKey(Resolve(key));
		}

		public void Flush()
		{
			PlayerPrefs.Save();
		}

		private string Resolve(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentException("A settings key is required.", nameof(key));
			}

			return _prefix + key;
		}
	}
}
