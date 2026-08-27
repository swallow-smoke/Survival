using System;
using System.Collections.Generic;

namespace AstraNope.Localization
{
    /// <summary>
    /// Central text lookup. All user-facing strings go through <see cref="T"/>/<see cref="F"/>.
    /// Resolves from the Localization "ui" table when available; otherwise falls back to the
    /// embedded Korean defaults in <see cref="UiText.Defaults"/> so the game never breaks
    /// before tables are generated/imported.
    /// </summary>
    public static class L10n
    {
        public const string TableName = "ui";

        internal static readonly IFormatProvider LocaleFormat =
            System.Globalization.CultureInfo.InvariantCulture;

        public static string T(string key)
        {
            if (!UiText.Defaults.TryGetValue(key, out var fallback))
                return key;
#if ENABLE_LOCALIZATION
            try
            {
                var resolved = UnityEngine.Localization.Settings.LocalizationSettings
                    .StringDatabase.GetLocalizedString(TableName, key);
                if (!string.IsNullOrEmpty(resolved) && resolved != key)
                    return resolved;
            }
            catch (Exception)
            {
                // Tables/locale not ready yet: serve the Korean default.
            }
#endif
            return fallback;
        }

        public static string F(string key, params object[] args)
        {
            var template = T(key);
            if (args == null || args.Length == 0) return template;
            try
            {
                return string.Format(LocaleFormat, template, args);
            }
            catch (FormatException)
            {
                return template;
            }
        }
    }
}
