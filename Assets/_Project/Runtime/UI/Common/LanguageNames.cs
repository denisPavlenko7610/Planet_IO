using System;
using System.Globalization;

namespace PlanetIO.UI
{
    public static class LanguageNames
    {
        public static string GetNativeName(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                return string.Empty;
            }

            try
            {
                CultureInfo culture = CultureInfo.GetCultureInfo(languageCode);
                string name = culture.NativeName;
                int bracket = name.IndexOf(" (", StringComparison.Ordinal);
                if (bracket > 0)
                {
                    name = name[..bracket];
                }

                return name.Length > 0 ? char.ToUpper(name[0], culture) + name[1..] : languageCode;
            }
            catch (CultureNotFoundException)
            {
                return languageCode;
            }
        }
    }
}
