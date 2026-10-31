using System;
using System.Threading;
using Pretzel.Core;
using Pretzel.Data;
using Pretzel.Preferences;
using UnityEngine;

namespace Pretzel.Samples.Preferences
{
    /// <summary>
    /// An <see cref="IPreferencesService"/> that never touches disk. Use it in tests, in a kiosk build that must forget
    /// everything on restart, or as a stand-in while the real backend is not ready.
    /// </summary>
    public sealed class InMemoryPreferencesService : IPreferencesService
    {
        private PlayerPreferences _saved;

        public PlayerPreferences Current { get; private set; } = new();

        public event Action<PlayerPreferences> Changed;

        public int SaveCount { get; private set; }

        public LoadResult<PlayerPreferences> Load()
        {
            LoadResult<PlayerPreferences> result = _saved == null
                ? LoadResult<PlayerPreferences>.NotFound()
                : LoadResult<PlayerPreferences>.Success(_saved.Clone());
            Current = result.Ok ? result.Value : new PlayerPreferences();
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
            _saved = Current.Clone();
            SaveCount++;
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
    }
}
