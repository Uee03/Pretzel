using System;
using Pretzel.Core;
using Pretzel.Preferences;
using UnityEngine;

namespace Pretzel.Samples.Preferences
{
    /// <summary>
    /// An editable copy of the live preferences. A menu edits <see cref="Values"/> freely, then commits them with one
    /// <see cref="IPreferencesService.Modify"/> call (Apply) or throws them away (Revert).
    /// </summary>
    public sealed class PreferencesDraft
    {
        private readonly IPreferencesService _preferences;
        private string _baseline;

        public PreferencesDraft(IPreferencesService preferences)
        {
            _preferences = Guard.NotNull(preferences, nameof(preferences));
            Reset();
        }

        public PlayerPreferences Values { get; private set; }

        public bool IsDirty => !string.Equals(JsonUtility.ToJson(Values), _baseline, StringComparison.Ordinal);

        /// <summary>Discards the edits and copies the live preferences again.</summary>
        public void Reset()
        {
            Values = _preferences.Current.Clone();
            _baseline = JsonUtility.ToJson(Values);
        }

        /// <summary>Writes the edits to the service, which raises Changed once.</summary>
        public void Commit(bool save = true)
        {
            PlayerPreferences snapshot = Values.Clone();
            _preferences.Modify(p =>
            {
                p.audio = snapshot.audio;
                p.graphics = snapshot.graphics;
                p.controls = snapshot.controls;
                p.language = snapshot.language;
            }, save);
        }
    }
}
