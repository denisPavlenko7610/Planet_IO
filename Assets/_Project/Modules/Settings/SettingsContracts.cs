using System;
using System.Collections.Generic;

namespace UnityTemplates.Settings
{
	public interface ISettingsStore
	{
		bool TryRead(string key, out string value);
		void Write(string key, string value);
		void Delete(string key);
		void Flush();
	}

	public interface ISettingCodec<T>
	{
		string Encode(T value);
		bool TryDecode(string value, out T result);
	}

	public interface ISettingsService
	{
		event Action<SettingChanged> Changed;

		T Get<T>(SettingKey<T> key);
		bool Set<T>(SettingKey<T> key, T value);
		bool Reset<T>(SettingKey<T> key);

		IDisposable BeginBatch();

		void Flush();
		void ClearCache();
	}

	public sealed class SettingKey<T>
	{
		private readonly Func<T, T> _normalize;

		public SettingKey(string id, T defaultValue, ISettingCodec<T> codec, Func<T, T> normalize = null)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				throw new ArgumentException("A setting id is required.", nameof(id));
			}

			Id = id.Trim();
			Codec = codec ?? throw new ArgumentNullException(nameof(codec));
			_normalize = normalize;

			DefaultValue = Normalize(defaultValue);

			EncodeValidated(DefaultValue);
		}

		public string Id { get; }

		public T DefaultValue { get; }

		public ISettingCodec<T> Codec { get; }

		public T Normalize(T value)
		{
			return _normalize == null
				? value
				: _normalize(value);
		}

		internal string EncodeValidated(T value)
		{
			string encoded = Codec.Encode(value);

			if (encoded == null)
			{
				throw new InvalidOperationException($"Setting '{Id}' codec returned null while encoding a value.");
			}

			if (!Codec.TryDecode(encoded, out T decoded) || !EqualityComparer<T>.Default.Equals(value, decoded))
			{
				throw new InvalidOperationException($"Setting '{Id}' codec cannot round-trip its value.");
			}

			return encoded;
		}
	}

	public readonly struct SettingChanged
	{
		public SettingChanged(string id, Type valueType, object previousValue, object currentValue)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				throw new ArgumentException("A setting id is required.", nameof(id));
			}

			Id = id.Trim();
			ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));

			PreviousValue = previousValue;
			CurrentValue = currentValue;
		}

		public string Id { get; }

		public Type ValueType { get; }

		public object PreviousValue { get; }

		public object CurrentValue { get; }
	}
}
