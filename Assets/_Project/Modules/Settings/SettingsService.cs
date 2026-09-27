using System;
using System.Collections.Generic;
using UnityEngine;
using UnityTemplates.Foundation;

namespace UnityTemplates.Settings
{
	public sealed class SettingsService : ISettingsService
	{
		private readonly ISettingsStore _store;

		private readonly Dictionary<string, object> _cache = new(StringComparer.Ordinal);
		private readonly Dictionary<string, Type> _knownTypes = new(StringComparer.Ordinal);
		private readonly Dictionary<string, SettingChanged> _batchedChanges = new(StringComparer.Ordinal);

		private int _batchDepth;

		public SettingsService(ISettingsStore store)
		{
			_store = store ?? throw new ArgumentNullException(nameof(store));
		}

		public event Action<SettingChanged> Changed;

		public T Get<T>(SettingKey<T> key)
		{
			Prepare(key);

			return GetCore(key);
		}

		public bool Set<T>(SettingKey<T> key, T value)
		{
			Prepare(key);

			T normalized = key.Normalize(value);

			string encoded = key.EncodeValidated(normalized);

			T current = GetCore(key);

			if (EqualityComparer<T>.Default.Equals(current, normalized))
			{
				return false;
			}

			_store.Write(key.Id, encoded);

			_cache[key.Id] = normalized;

			QueueOrRaise(new SettingChanged(key.Id, typeof(T), current, normalized));

			return true;
		}

		public bool Reset<T>(SettingKey<T> key)
		{
			Prepare(key);

			T current = GetCore(key);

			T defaultValue = key.DefaultValue;

			_store.Delete(key.Id);

			_cache[key.Id] = defaultValue;

			if (EqualityComparer<T>.Default.Equals(current, defaultValue))
			{
				return false;
			}

			QueueOrRaise(new SettingChanged(key.Id, typeof(T), current, defaultValue));

			return true;
		}

		public IDisposable BeginBatch()
		{
			_batchDepth++;

			return new BatchScope(this);
		}

		public void Flush()
		{
			_store.Flush();
		}

		public void ClearCache()
		{
			if (_batchDepth != 0)
			{
				throw new InvalidOperationException("Cannot clear the settings cache during a batch.");
			}

			_cache.Clear();
		}

		private T GetCore<T>(SettingKey<T> key)
		{
			if (_cache.TryGetValue(key.Id, out object cached))
			{
				return (T)cached;
			}

			if (!_store.TryRead(key.Id, out string encoded))
			{
				return CacheDefault(key);
			}

			if (!key.Codec.TryDecode(encoded, out T decoded))
			{
				_store.Delete(key.Id);

				return CacheDefault(key);
			}

			T normalized = key.Normalize(decoded);

			string canonical = key.EncodeValidated(normalized);

			if (!string.Equals(encoded, canonical, StringComparison.Ordinal))
			{
				_store.Write(key.Id, canonical);
			}

			_cache[key.Id] = normalized;

			return normalized;
		}

		private T CacheDefault<T>(SettingKey<T> key)
		{
			T value = key.DefaultValue;

			_cache[key.Id] = value;

			return value;
		}

		private void QueueOrRaise(SettingChanged change)
		{
			if (_batchDepth == 0)
			{
				SafeEvent.Raise(Changed, change);
				return;
			}

			if (_batchedChanges.TryGetValue(change.Id, out SettingChanged existing))
			{
				_batchedChanges[change.Id] = new SettingChanged(
					change.Id,
					change.ValueType,
					existing.PreviousValue,
					change.CurrentValue
				);
			}
			else
			{
				_batchedChanges.Add(change.Id, change);
			}
		}

		private void EndBatch()
		{
			if (_batchDepth <= 0)
			{
				throw new InvalidOperationException("Settings batch scope is unbalanced.");
			}

			_batchDepth--;

			if (_batchDepth != 0)
			{
				return;
			}

			List<SettingChanged> changes = new(_batchedChanges.Values);

			_batchedChanges.Clear();

			changes.Sort((left, right) =>
				string.CompareOrdinal(left.Id, right.Id)
			);

			foreach (SettingChanged change in changes)
			{
				if (!Equals(change.PreviousValue, change.CurrentValue))
				{
					SafeEvent.Raise(Changed, change);
				}
			}
		}

		private void Prepare<T>(SettingKey<T> key)
		{
			ValidateKey(key);
			RegisterType(key.Id, typeof(T));
		}

		private void RegisterType(string id, Type type)
		{
			if (_knownTypes.TryGetValue(id, out Type knownType))
			{
				if (knownType != type)
				{
					throw new InvalidOperationException(
						$"Setting '{id}' was already used as " + $"{knownType.FullName}, not {type.FullName}."
					);
				}

				return;
			}

			_knownTypes.Add(id, type);
		}

		private static void ValidateKey<T>(SettingKey<T> key)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}
		}

		private sealed class BatchScope : IDisposable
		{
			private SettingsService _owner;

			public BatchScope(SettingsService owner)
			{
				_owner = owner;
			}

			public void Dispose()
			{
				SettingsService owner = _owner;

				if (owner == null)
				{
					return;
				}

				_owner = null;

				owner.EndBatch();
			}
		}
	}
}
