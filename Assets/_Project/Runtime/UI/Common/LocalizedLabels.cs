using System;
using System.Collections.Generic;
using RD.Localization;
using UnityEngine.UIElements;

namespace PlanetIO.UI
{
    public sealed class LocalizedLabels : IDisposable
    {
        private readonly ILocalizationService _localization;
        private static readonly (string Prefix, string StyleClass)[] LanguageClasses =
        {
            ("zh", "lang-zh"),
            ("ja", "lang-ja"),
            ("ko", "lang-ko")
        };

        private readonly List<(Action<string> Apply, string Key)> _bindings = new();
        private readonly List<VisualElement> _languageRoots = new();

        public LocalizedLabels(ILocalizationService localization)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _localization.LocaleChanged += OnLocaleChanged;
        }

        public void Bind(TextElement element, string key)
        {
            if (element != null)
            {
                Bind(text => element.text = text, key);
            }
        }

        public void BindPlaceholder(TextField field, string key)
        {
            if (field != null)
            {
                Bind(text => field.textEdition.placeholder = text, key);
            }
        }

        public void BindLanguageClass(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            _languageRoots.Add(root);
            ApplyLanguageClass(root);
        }

        public void Bind(Action<string> apply, string key)
        {
            _bindings.Add((apply, key));
            apply(_localization.Get(key));
        }

        public void Dispose()
        {
            _localization.LocaleChanged -= OnLocaleChanged;
            _bindings.Clear();
            _languageRoots.Clear();
        }

        private void ApplyLanguageClass(VisualElement root)
        {
            string locale = _localization.CurrentLocale ?? string.Empty;
            foreach ((string prefix, string styleClass) in LanguageClasses)
            {
                root.EnableInClassList(styleClass, locale.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            }
        }

        private void OnLocaleChanged(LocaleChanged _)
        {
            foreach (VisualElement root in _languageRoots)
            {
                ApplyLanguageClass(root);
            }

            foreach ((Action<string> apply, string key) in _bindings)
            {
                apply(_localization.Get(key));
            }
        }
    }
}
