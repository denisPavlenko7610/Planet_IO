using System;
using System.Globalization;

namespace UnityTemplates.Settings
{
	public static class SettingCodecs
	{
		public static ISettingCodec<string> String { get; } = new StringCodec();

		public static ISettingCodec<int> Int32 { get; } = new Int32Codec();

		public static ISettingCodec<long> Int64 { get; } = new Int64Codec();

		public static ISettingCodec<float> Single { get; } = new SingleCodec();

		public static ISettingCodec<double> Double { get; } = new DoubleCodec();

		public static ISettingCodec<bool> Boolean { get; } = new BooleanCodec();

		public static ISettingCodec<TEnum> Enum<TEnum>() where TEnum : struct, Enum
		{
			return EnumCodec<TEnum>.Instance;
		}

		private sealed class StringCodec : ISettingCodec<string>
		{
			public string Encode(string value)
			{
				return value == null
					? "0"
					: "1" + value;
			}

			public bool TryDecode(string value, out string result)
			{
				if (value == "0")
				{
					result = null;
					return true;
				}

				if (!string.IsNullOrEmpty(value) && value[0] == '1')
				{
					result = value[1..];
					return true;
				}

				result = null;
				return false;
			}
		}

		private sealed class Int32Codec : ISettingCodec<int>
		{
			public string Encode(int value)
			{
				return value.ToString(CultureInfo.InvariantCulture);
			}

			public bool TryDecode(string value, out int result)
			{
				return int.TryParse(
					value,
					NumberStyles.Integer,
					CultureInfo.InvariantCulture,
					out result
				);
			}
		}

		private sealed class Int64Codec : ISettingCodec<long>
		{
			public string Encode(long value)
			{
				return value.ToString(CultureInfo.InvariantCulture);
			}

			public bool TryDecode(string value, out long result)
			{
				return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
			}
		}

		private sealed class SingleCodec : ISettingCodec<float>
		{
			public string Encode(float value)
			{
				ValidateFinite(value);

				return value.ToString("R", CultureInfo.InvariantCulture);
			}

			public bool TryDecode(string value, out float result)
			{
				return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result)
					&& !float.IsNaN(result)
					&& !float.IsInfinity(result);
			}

			private static void ValidateFinite(float value)
			{
				if (float.IsNaN(value) || float.IsInfinity(value))
				{
					throw new ArgumentOutOfRangeException(
						nameof(value),
						"A setting value must be finite."
					);
				}
			}
		}

		private sealed class DoubleCodec : ISettingCodec<double>
		{
			public string Encode(double value)
			{
				ValidateFinite(value);

				return value.ToString("R", CultureInfo.InvariantCulture);
			}

			public bool TryDecode(string value, out double result)
			{
				return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result)
					&& !double.IsNaN(result)
					&& !double.IsInfinity(result);
			}

			private static void ValidateFinite(double value)
			{
				if (double.IsNaN(value) || double.IsInfinity(value))
				{
					throw new ArgumentOutOfRangeException(nameof(value), "A setting value must be finite.");
				}
			}
		}

		private sealed class BooleanCodec : ISettingCodec<bool>
		{
			public string Encode(bool value)
			{
				return value ? "1" : "0";
			}

			public bool TryDecode(string value, out bool result)
			{
				if (value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
				{
					result = true;
					return true;
				}

				if (value == "0" || string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
				{
					result = false;
					return true;
				}

				result = false;
				return false;
			}
		}

		private sealed class EnumCodec<TEnum> : ISettingCodec<TEnum>
			where TEnum : struct, Enum
		{
			public static readonly EnumCodec<TEnum> Instance = new();

			public string Encode(TEnum value)
			{
				if (!IsValid(value))
				{
					throw new ArgumentOutOfRangeException(nameof(value), value, "The enum value is not defined.");
				}

				return System.Enum.Format(typeof(TEnum), value, "D");
			}

			public bool TryDecode(string value, out TEnum result)
			{
				return System.Enum.TryParse(value, true, out result) && IsValid(result);
			}

			private static bool IsValid(TEnum value)
			{
				Type enumType = typeof(TEnum);

				if (System.Enum.IsDefined(enumType, value))
				{
					return true;
				}

				if (!Attribute.IsDefined(enumType, typeof(FlagsAttribute)))
				{
					return false;
				}

				ulong knownBits = 0UL;

				foreach (object definedValue in System.Enum.GetValues(enumType))
				{
					knownBits |= ToUInt64Bits((TEnum)definedValue);
				}

				ulong bits = ToUInt64Bits(value);

				return (bits & ~knownBits) == 0UL;
			}

			private static ulong ToUInt64Bits(TEnum value)
			{
				TypeCode typeCode = Type.GetTypeCode(System.Enum.GetUnderlyingType(typeof(TEnum)));

				return typeCode switch
				{
					TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 => unchecked(
						(ulong)Convert.ToInt64(value, CultureInfo.InvariantCulture)),
					_ => Convert.ToUInt64(value, CultureInfo.InvariantCulture)
				};
			}
		}
	}
}
