using System;
using System.Threading;
using Pretzel.Data;
using UnityEngine;

namespace Pretzel.Preferences
{
    /// <summary>
    /// The contract that options menus, audio bridges and other consumers depend on. <see cref="PreferencesService"/> is the
    /// default implementation on Pretzel Data; implement this interface to keep preferences somewhere else, such as
    /// PlayerPrefs, a platform cloud or a remote profile.
    /// </summary>
    public interface IPreferencesService
    {
        /// <summary>The live preferences. Never null.</summary>
        PlayerPreferences Current { get; }

        /// <summary>Raised after every load, modification and reset, with <see cref="Current"/>.</summary>
        event Action<PlayerPreferences> Changed;

        /// <summary>Loads from the backing store. A missing or unreadable store falls back to defaults and the result says which.</summary>
        LoadResult<PlayerPreferences> Load();

        Awaitable<LoadResult<PlayerPreferences>> LoadAsync(CancellationToken cancellationToken = default);

        void Save();

        Awaitable SaveAsync(CancellationToken cancellationToken = default);

        /// <summary>Changes <see cref="Current"/> in place, raises <see cref="Changed"/> and optionally saves.</summary>
        void Modify(Action<PlayerPreferences> change, bool save = false);

        void ResetToDefaults(bool save = false);

        /// <summary>Applies the graphics section (quality, vSync, frame rate and, on desktop, resolution).</summary>
        void ApplyGraphics();
    }
}
