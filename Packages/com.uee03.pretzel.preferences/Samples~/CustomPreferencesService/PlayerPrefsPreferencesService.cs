using System;
using System.Threading;
using Pretzel.Core;
using Pretzel.Data;
using Pretzel.Preferences;
using UnityEngine;

namespace Pretzel.Samples.Preferences
{
    /// <summary>
    /// An <see cref="IPreferencesService"/> that keeps the preferences as one JSON string in PlayerPrefs. It needs no
    /// Pretzel Data store, which suits small projects and platforms where PlayerPrefs is synced for you.
    /// </summary>
    public sealed class PlayerPrefsPreferencesService : IPreferencesService
    {
        private readonly string _key;

        public PlayerPrefsPreferencesService(string key = "pretzel.preferences")
        {
            _key = Guard.NotNullOrEmpty(key, nameof(key));
        }

        public PlayerPreferences Current { get; private set; } = new();

        public event Action<PlayerPreferences> Changed;

        public LoadResult<PlayerPreferences> Load()
        {
            LoadResult<PlayerPreferences> result = Read();
            Current = result.Ok && result.Value != null ? Normalize(result.Value) : new PlayerPreferences();
            Changed?.Invoke(Current);
            return result;
        }

        public Awaitable<LoadResult<PlayerPreferences>> LoadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AwaitableCompletionSource<LoadResult<PlayerPreferences>> completion = new();
            completion.SetResult(Load());
            return completion.Awaitable;
        }

        public void Save()
        {
            PlayerPrefs.SetString(_key, JsonUtility.ToJson(Current));
            PlayerPrefs.Save();
        }

        public Awaitable SaveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Save();
            AwaitableCompletionSource completion = new();
            completion.SetResult();
            return completion.Awaitable;
        }

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

        public void ApplyGraphics() => Current.graphics.Apply();

        private LoadResult<PlayerPreferences> Read()
        {
            if (!PlayerPrefs.HasKey(_key))
            {
                return LoadResult<PlayerPreferences>.NotFound();
            }

            try
            {
                PlayerPreferences loaded = JsonUtility.FromJson<PlayerPreferences>(PlayerPrefs.GetString(_key));
                return loaded != null ? LoadResult<PlayerPreferences>.Success(loaded) : LoadResult<PlayerPreferences>.Corrupt("The stored preferences are empty.");
            }
            catch (ArgumentException ex)
            {
                return LoadResult<PlayerPreferences>.Corrupt(ex.Message);
            }
        }

        // Older strings can lack whole sections.
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
