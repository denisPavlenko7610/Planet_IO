using System;
using System.Collections.Generic;

namespace UnityTemplates.Localization
{
	internal static class LocalizationValidators
	{
		public static string ValidateKey(string key)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			if (key.Length == 0)
			{
				throw new ArgumentException("Localization key cannot be empty.");
			}

			return key;
		}

		public static string ValidateLocale(string locale)
		{
			if (locale == null)
			{
				throw new ArgumentNullException(nameof(locale));
			}

			return TrimAndValidate(locale, "Locale cannot be empty.");
		}

		public static bool IsSpecified(string value)
		{
			return !string.IsNullOrWhiteSpace(value);
		}

		private static string TrimAndValidate(string value, string emptyMessage)
		{
			string trimmed = value.Trim();

			if (trimmed.Length == 0)
			{
				throw new ArgumentException(emptyMessage);
			}

			return trimmed;
		}
	}

	public static class LocaleResolver
	{
		internal static void ValidateAvailableLocales(IReadOnlyCollection<string> availableLocales)
		{
			if (availableLocales == null)
			{
				throw new ArgumentNullException(nameof(availableLocales));
			}

			Dictionary<string, string> declaredLocales = new(StringComparer.OrdinalIgnoreCase);

			foreach (string locale in availableLocales)
			{
				string normalized = Normalize(locale);

				if (declaredLocales.TryGetValue(normalized, out string existing))
				{
					throw new ArgumentException(
						$"Locales '{existing}' and '{locale}' are equivalent. "
						+ "Declare only one spelling for each locale.",
						nameof(availableLocales)
					);
				}

				declaredLocales.Add(normalized, locale);
			}
		}

		public static string Resolve(string requestedLocale, IReadOnlyCollection<string> availableLocales)
		{
			if (availableLocales == null)
			{
				throw new ArgumentNullException(nameof(availableLocales));
			}

			string candidate = Normalize(requestedLocale);

			while (!string.IsNullOrEmpty(candidate))
			{
				foreach (string available in availableLocales)
				{
					if (SameLocale(candidate, available))
					{
						return available;
					}
				}

				int separator = candidate.LastIndexOf('-');

				if (separator <= 0)
				{
					break;
				}

				candidate = candidate[..separator];
			}

			return null;
		}

		public static bool SameLocale(string left, string right)
		{
			return string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);
		}

		private static string Normalize(string locale)
		{
			return LocalizationValidators
				.ValidateLocale(locale)
				.Replace('_', '-');
		}
	}
}
