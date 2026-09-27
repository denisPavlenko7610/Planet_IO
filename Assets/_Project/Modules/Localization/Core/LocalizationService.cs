using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityTemplates.Foundation;

namespace UnityTemplates.Localization
{
	public sealed class LocalizationService : ILocalizationService
	{
		private static readonly object[] EmptyArguments = Array.Empty<object>();

		private readonly ILocalizationProvider _provider;
		private readonly Func<string, string> _missingKeyFormatter;
		private readonly AsyncGate _localeChangeGate = new();

		private bool _isDisposed;

		public LocalizationService(ILocalizationProvider provider, Func<string, string> missingKeyFormatter = null)
		{
			_provider = ValidateProvider(provider);
			_missingKeyFormatter = missingKeyFormatter ?? (key => $"[{key}]");
			_provider.LocaleChanged += OnProviderLocaleChanged;
		}

		public string CurrentLocale
		{
			get
			{
				ThrowIfDisposed();
				return _provider.CurrentLocale;
			}
		}

		public IReadOnlyCollection<string> AvailableLocales
		{
			get
			{
				ThrowIfDisposed();
				return _provider.AvailableLocales;
			}
		}

		public bool IsChangingLocale { get; private set; }

		public event Action<LocaleChanged> LocaleChanged;

		public event Action<LocalizationIssue> IssueDetected;

		public string Get(string key)
		{
			return Get(key, EmptyArguments);
		}

		public string Get(string key, params object[] arguments)
		{
			string validatedKey = LocalizationValidators.ValidateKey(key);

			return TryGetValidated(validatedKey, arguments, out string text)
				? text
				: _missingKeyFormatter(validatedKey);
		}

		public bool TryGet(string key, out string text)
		{
			return TryGet(key, out text, EmptyArguments);
		}

		public bool TryGet(string key, out string text, params object[] arguments)
		{
			string validatedKey = LocalizationValidators.ValidateKey(key);
			return TryGetValidated(validatedKey, arguments, out text);
		}

		public async Awaitable SetLocaleAsync(string locale, CancellationToken cancellationToken = default)
		{
			ThrowIfDisposed();

			string requestedLocale = LocalizationValidators.ValidateLocale(locale);
			string resolvedLocale = LocaleResolver.Resolve(requestedLocale, AvailableLocales);

			if (resolvedLocale == null)
			{
				throw new LocaleNotAvailableException(requestedLocale, AvailableLocales);
			}

			using (await _localeChangeGate.EnterAsync(cancellationToken))
			{
				ThrowIfDisposed();

				if (LocaleResolver.SameLocale(CurrentLocale, resolvedLocale))
				{
					return;
				}

				IsChangingLocale = true;

				try
				{
					await _provider.SetLocaleAsync(resolvedLocale, cancellationToken);
					cancellationToken.ThrowIfCancellationRequested();

					if (!LocaleResolver.SameLocale(CurrentLocale, resolvedLocale))
					{
						throw new InvalidOperationException(
							$"Provider did not switch to '{resolvedLocale}'. " + $"Current locale is '{CurrentLocale}'."
						);
					}
				}
				finally
				{
					IsChangingLocale = false;
				}
			}
		}

		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}

			_provider.LocaleChanged -= OnProviderLocaleChanged;
			_isDisposed = true;
		}

		private bool TryGetValidated(string key, object[] arguments, out string text)
		{
			ThrowIfDisposed();

			try
			{
				if (_provider.TryGet(key, arguments ?? EmptyArguments, out text) && text != null)
				{
					return true;
				}
			}
			catch (FormatException exception)
			{
				text = null;
				RaiseIssue(key, LocalizationIssueKind.InvalidFormat, exception);
				return false;
			}

			text = null;
			RaiseIssue(key, LocalizationIssueKind.MissingTranslation);
			return false;
		}

		private static ILocalizationProvider ValidateProvider(ILocalizationProvider provider)
		{
			if (provider == null)
			{
				throw new ArgumentNullException(nameof(provider));
			}

			if (!LocalizationValidators.IsSpecified(provider.CurrentLocale))
			{
				throw new ArgumentException(
					"Provider must expose an initialized current locale.",
					nameof(provider)
				);
			}

			if (provider.AvailableLocales == null)
			{
				throw new ArgumentException(
					"Provider must expose its available locales.",
					nameof(provider)
				);
			}

			LocaleResolver.ValidateAvailableLocales(provider.AvailableLocales);

			if (LocaleResolver.Resolve(provider.CurrentLocale, provider.AvailableLocales) == null)
			{
				throw new ArgumentException(
					"Provider current locale must be available.",
					nameof(provider)
				);
			}

			return provider;
		}

		private void OnProviderLocaleChanged(LocaleChanged change)
		{
			SafeEvent.Raise(LocaleChanged, change);
		}

		private void RaiseIssue(string key, LocalizationIssueKind kind, Exception exception = null)
		{
			SafeEvent.Raise(IssueDetected, new LocalizationIssue(CurrentLocale, key, kind, exception));
		}

		private void ThrowIfDisposed()
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(LocalizationService));
			}
		}
	}
}
