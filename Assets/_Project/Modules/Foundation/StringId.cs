using System;
using UnityEngine;

namespace UnityTemplates.Foundation
{
	[Serializable]
	public struct StringId : IEquatable<StringId>, IComparable<StringId>
	{
		public const int MaxLength = 128;

		[SerializeField] private string _value;

		public StringId(string value)
		{
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			if (!IsValidValue(value))
			{
				throw new ArgumentException(
					$"An identifier must contain 1 to {MaxLength} non-whitespace, non-control characters.",
					nameof(value)
				);
			}

			_value = value;
		}

		public string Value => _value ?? string.Empty;

		public bool IsValid => IsValidValue(_value);

		public static bool TryCreate(string value, out StringId id)
		{
			if (!IsValidValue(value))
			{
				id = default;
				return false;
			}

			id = new StringId(value);
			return true;
		}

		public static bool IsValidValue(string value)
		{
			if (string.IsNullOrEmpty(value) || value.Length > MaxLength)
			{
				return false;
			}

			for (int index = 0; index < value.Length; index++)
			{
				if (char.IsControl(value[index]) || char.IsWhiteSpace(value[index]))
				{
					return false;
				}
			}

			return true;
		}

		public static string Require(string value, string parameterName)
		{
			if (value == null)
			{
				throw new ArgumentNullException(parameterName);
			}

			if (!IsValidValue(value))
			{
				throw new ArgumentException(
					$"An identifier must contain 1 to {MaxLength} non-whitespace, non-control characters.",
					parameterName
				);
			}

			return value;
		}

		public bool Equals(StringId other)
		{
			return string.Equals(_value, other._value, StringComparison.Ordinal);
		}

		public override bool Equals(object obj)
		{
			return obj is StringId other && Equals(other);
		}

		public override int GetHashCode()
		{
			return _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
		}

		public int CompareTo(StringId other)
		{
			return string.Compare(_value, other._value, StringComparison.Ordinal);
		}

		public override string ToString()
		{
			return Value;
		}

		public static bool operator ==(StringId left, StringId right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(StringId left, StringId right)
		{
			return !left.Equals(right);
		}
	}
}
