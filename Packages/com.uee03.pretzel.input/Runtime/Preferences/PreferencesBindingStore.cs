using System;
using Pretzel.Preferences;

namespace Pretzel.Inputs.Preferences
{
    /// <summary>Keeps binding overrides in Pretzel Preferences, next to the player's other control settings, so they save and reset together.</summary>
    public sealed class PreferencesBindingStore : IBindingStore
    {
        private readonly IPreferencesService _preferences;

        public PreferencesBindingStore(IPreferencesService preferences)
        {
            _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        }

        public string Load()
        {
            return _preferences.Current.controls.bindingOverridesJson ?? string.Empty;
        }

        public void Save(string json)
        {
            _preferences.Modify(preferences => preferences.controls.bindingOverridesJson = json ?? string.Empty, save: true);
        }
    }
}
