using System;
using Pretzel.Preferences;
using UnityEngine;

namespace Pretzel.Samples.Preferences
{
    /// <summary>
    /// Resolves the language code (an empty preference follows the system language) and raises <see cref="LanguageChanged"/>
    /// only when it really changed. Connect your localization system to the event.
    /// </summary>
    public sealed class LanguageApplier : IPreferenceApplier
    {
        private string _current;

        public string Current => _current;

        public event Action<string> LanguageChanged;

        public void Apply(PlayerPreferences preferences)
        {
            string code = string.IsNullOrEmpty(preferences.language) ? FromSystem() : preferences.language;
            if (string.Equals(code, _current, StringComparison.Ordinal))
            {
                return;
            }

            _current = code;
            LanguageChanged?.Invoke(code);
        }

        private static string FromSystem()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Portuguese:
                    return "pt-BR";
                case SystemLanguage.German:
                    return "de";
                case SystemLanguage.French:
                    return "fr";
                case SystemLanguage.Spanish:
                    return "es";
                case SystemLanguage.Japanese:
                    return "ja";
                default:
                    return "en";
            }
        }
    }
}
