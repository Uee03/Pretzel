using System;
using System.Threading;
using Pretzel.Core;
using Pretzel.Data;
using UnityEngine;

namespace Pretzel.Preferences
{
    /// <summary>
    /// Owns the current <see cref="PlayerPreferences"/>, loads and saves them through an <see cref="IDataStore"/>, and raises
    /// <see cref="Changed"/> after every load or change. A missing or unreadable file falls back to defaults; the load result
    /// tells you which happened, so a corrupt file can be reported instead of silently replaced.
    /// </summary>
    public sealed class PreferencesService
    {
        public static readonly DataKey DefaultKey = new("preferences");

        private readonly IDataStore _store;
        private readonly DataKey _key;

        public PlayerPreferences Current { get; private set; } = new();

        public event Action<PlayerPreferences> Changed;

        /// <param name="store">Defaults to <see cref="DataIO.Default"/>, resolved on each call.</param>
        public PreferencesService(IDataStore store = null, DataKey? key = null)
        {
            _store = store;
            _key = key ?? DefaultKey;
        }

        private IDataStore Store => _store ?? DataIO.Default;

        public LoadResult<PlayerPreferences> Load()
        {
            LoadResult<PlayerPreferences> result = Store.Load<PlayerPreferences>(_key);
            Accept(result);
            return result;
        }

        public async Awaitable<LoadResult<PlayerPreferences>> LoadAsync(CancellationToken cancellationToken = default)
        {
            LoadResult<PlayerPreferences> result = await Store.LoadAsync<PlayerPreferences>(_key, cancellationToken);
            Accept(result);
            return result;
        }

        public void Save() => Store.Save(_key, Current);

        public Awaitable SaveAsync(CancellationToken cancellationToken = default) => Store.SaveAsync(_key, Current.Clone(), cancellationToken);

        /// <summary>Changes the current preferences in place and raises <see cref="Changed"/>.</summary>
        public void Modify(Action<PlayerPreferences> change, bool save = false)
        {
            Guard.NotNull(change, nameof(change));
            change(Current);
            Changed?.Invoke(Current);
            if (save)
            {
                Save();
            }
        }

        public void ResetToDefaults(bool save = false)
        {
            Current = new PlayerPreferences();
            Changed?.Invoke(Current);
            if (save)
            {
                Save();
            }
        }

        /// <summary>Applies quality, vSync, frame rate and, on desktop, resolution.</summary>
        public void ApplyGraphics() => Current.graphics.Apply();

        private void Accept(LoadResult<PlayerPreferences> result)
        {
            Current = result.Ok && result.Value != null ? Normalize(result.Value) : new PlayerPreferences();
            Changed?.Invoke(Current);
        }

        // Files from an older version can lack whole sections.
        private static PlayerPreferences Normalize(PlayerPreferences loaded)
        {
            loaded.audio ??= new AudioPreferences();
            loaded.graphics ??= new GraphicsPreferences();
            loaded.controls ??= new ControlPreferences();
            loaded.language ??= string.Empty;
            return loaded;
        }
    }
}
