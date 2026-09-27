using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace UnityTemplates.Localization
{
	public interface ILocalizationProvider
	{
		string CurrentLocale { get; }

		IReadOnlyCollection<string> AvailableLocales { get; }

		event Action<LocaleChanged> LocaleChanged;

		bool TryGet(string key, object[] arguments, out string text);

		Awaitable SetLocaleAsync(string locale, CancellationToken cancellationToken = default);
	}
}
