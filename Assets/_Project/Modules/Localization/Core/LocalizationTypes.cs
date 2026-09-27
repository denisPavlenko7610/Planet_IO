using System;
using System.Collections.Generic;

namespace UnityTemplates.Localization
{
	public readonly struct LocaleChanged
	{
		public LocaleChanged(string previousLocale, string currentLocale)
		{
			PreviousLocale = previousLocale;
			CurrentLocale = currentLocale;
		}

		public string PreviousLocale { get; }

		public string CurrentLocale { get; }
	}

	public enum LocalizationIssueKind
	{
		MissingTranslation,
		InvalidFormat
	}

	public readonly struct LocalizationIssue
	{
		public LocalizationIssue(string locale, string key, LocalizationIssueKind kind, Exception exception = null)
		{
			Locale = locale;
			Key = key;
			Kind = kind;
			Exception = exception;
		}

		public string Locale { get; }

		public string Key { get; }

		public LocalizationIssueKind Kind { get; }

		public Exception Exception { get; }
	}

	public sealed class LocaleNotAvailableException : Exception
	{
		public LocaleNotAvailableException(string requestedLocale, IReadOnlyCollection<string> availableLocales)
			: base(BuildMessage(requestedLocale, availableLocales))
		{
			RequestedLocale = requestedLocale;
			AvailableLocales = availableLocales;
		}

		public string RequestedLocale { get; }

		public IReadOnlyCollection<string> AvailableLocales { get; }

		private static string BuildMessage(string requestedLocale, IReadOnlyCollection<string> availableLocales)
		{
			return
				$"Locale '{requestedLocale}' is not available. "
				+ $"Available locales: {string.Join(", ", availableLocales)}.";
		}
	}
}
