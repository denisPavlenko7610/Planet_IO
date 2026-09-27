using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.Core.Formatting;
using UnityEngine.Localization.Tables;
using UnityTemplates.Localization;

namespace PlanetIO.Infrastructure.Localization
{
    public sealed class UnityLocalizationProvider : ILocalizationProvider, IDisposable
    {
        private readonly LocalizedStringTable _stringTable = new(LocalizationKeys.TableName);
        private readonly List<string> _availableLocales = new();

        private StringTable _currentTable;
        private bool _isDisposed;
        private bool _isSettingLocale;

        public UnityLocalizationProvider()
        {
            _stringTable.TableChanged += OnTableChanged;
        }

        public string CurrentLocale { get; private set; }

        public IReadOnlyCollection<string> AvailableLocales
        {
            get
            {
                _availableLocales.Clear();
                foreach (Locale locale in LocalizationSettings.AvailableLocales.Locales)
                {
                    _availableLocales.Add(locale.Identifier.Code);
                }

                return _availableLocales;
            }
        }

        public event Action<LocaleChanged> LocaleChanged;

        public void Initialize()
        {
            var initialization = LocalizationSettings.InitializationOperation;
            if (!initialization.IsDone)
            {
                initialization.WaitForCompletion();
            }

            Locale locale = LocalizationSettings.SelectedLocale ?? LocalizationSettings.ProjectLocale;
            StringTable table = LocalizationSettings.StringDatabase.GetTable(_stringTable.TableReference, locale);
            if (table == null)
            {
                throw new InvalidOperationException($"Localization table '{LocalizationKeys.TableName}' could not be loaded.");
            }

            ApplyTable(table);
        }

        public bool TryGet(string key, object[] arguments, out string text)
        {
            ThrowIfDisposed();

            if (_currentTable == null)
            {
                throw new InvalidOperationException("Localization table is not loaded. Initialize the provider first.");
            }

            StringTableEntry entry = _currentTable.GetEntry(key);
            if (entry == null || string.IsNullOrEmpty(entry.Value))
            {
                entry = LocalizationSettings.StringDatabase.GetTableEntry(
                    _stringTable.TableReference, key, FindLocale(CurrentLocale), FallbackBehavior.UseProjectSettings).Entry;
            }

            if (entry == null)
            {
                text = null;
                return false;
            }

            try
            {
                text = entry.GetLocalizedString(arguments);
            }
            catch (FormattingException exception)
            {
                throw new FormatException($"Localized string '{key}' has an invalid format.", exception);
            }

            return text != null;
        }

        public async Awaitable SetLocaleAsync(string locale, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            IReadOnlyCollection<string> availableLocales = AvailableLocales;
            string resolvedLocale = LocaleResolver.Resolve(locale, availableLocales);
            if (resolvedLocale == null)
            {
                throw new LocaleNotAvailableException(locale, availableLocales);
            }

            if (_currentTable != null && LocaleResolver.SameLocale(CurrentLocale, resolvedLocale))
            {
                return;
            }

            Locale targetLocale = FindLocale(resolvedLocale);
            StringTable table = await LocalizationSettings.StringDatabase
                .GetTableAsync(_stringTable.TableReference, targetLocale)
                .Task;

            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (table == null)
            {
                throw new InvalidOperationException(
                    $"Localization table '{LocalizationKeys.TableName}' could not be loaded for '{resolvedLocale}'.");
            }

            _isSettingLocale = true;
            try
            {
                SystemFontFallback.EnsureFor(resolvedLocale);
                LocalizationSettings.SelectedLocale = targetLocale;
                ApplyTable(table);
            }
            finally
            {
                _isSettingLocale = false;
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _stringTable.TableChanged -= OnTableChanged;
            _isDisposed = true;
        }

        private void OnTableChanged(StringTable table)
        {
            if (!_isDisposed && !_isSettingLocale && TableMatchesSelectedLocale(table))
            {
                ApplyTable(table);
            }
        }

        private void ApplyTable(StringTable table)
        {
            string nextLocale = table.LocaleIdentifier.Code;
            string previousLocale = CurrentLocale;

            SystemFontFallback.EnsureFor(nextLocale);
            _currentTable = table;
            CurrentLocale = nextLocale;

            if (string.IsNullOrEmpty(previousLocale) || !LocaleResolver.SameLocale(previousLocale, nextLocale))
            {
                LocaleChanged?.Invoke(new LocaleChanged(previousLocale, nextLocale));
            }
        }

        private static bool TableMatchesSelectedLocale(StringTable table)
        {
            return table != null &&
                   LocalizationSettings.SelectedLocale != null &&
                   LocaleResolver.SameLocale(table.LocaleIdentifier.Code, LocalizationSettings.SelectedLocale.Identifier.Code);
        }

        private Locale FindLocale(string locale)
        {
            foreach (Locale candidate in LocalizationSettings.AvailableLocales.Locales)
            {
                if (LocaleResolver.SameLocale(candidate.Identifier.Code, locale))
                {
                    return candidate;
                }
            }

            throw new LocaleNotAvailableException(locale, AvailableLocales);
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(UnityLocalizationProvider));
            }
        }
    }
}
