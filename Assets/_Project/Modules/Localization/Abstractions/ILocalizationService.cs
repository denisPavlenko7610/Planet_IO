using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace UnityTemplates.Localization
{
	public interface ILocalizationService : IDisposable
	{
		string CurrentLocale { get; }

		IReadOnlyCollection<string> AvailableLocales { get; }

		bool IsChangingLocale { get; }

		event Action<LocaleChanged> LocaleChanged;

		event Action<LocalizationIssue> IssueDetected;

		string Get(string key);

		string Get(string key, params object[] arguments);

		bool TryGet(string key, out string text);

		bool TryGet(string key, out string text, params object[] arguments);

		Awaitable SetLocaleAsync(string locale, CancellationToken cancellationToken = default);
	}
}
