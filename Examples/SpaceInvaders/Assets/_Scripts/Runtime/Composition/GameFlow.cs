using System;
using System.IO;
using System.Threading;
using Pretzel.Audio;
using Pretzel.Data;
using Pretzel.Preferences;
using UnityEngine;
using VContainer.Unity;

namespace SpaceInvaders
{
    /// <summary>Runs once when the game starts: loads the player's preferences and saved progress, and keeps the audio volumes in step.</summary>
    public sealed class GameFlow : IAsyncStartable, IDisposable
    {
        private readonly IGameService _game;
        private readonly IPreferencesService _preferences;
        private readonly IAudioService _audio;

        public GameFlow(IGameService game, IPreferencesService preferences, IAudioService audio)
        {
            _game = game;
            _preferences = preferences;
            _audio = audio;
        }

        public async Awaitable StartAsync(CancellationToken cancellation = default)
        {
            _preferences.Changed += ApplyAudio;

            // Preferences load first: the title screen shows the saved volume as soon as the progress is loaded.
            LoadResult<PlayerPreferences> preferences = await _preferences.LoadAsync(cancellation);
            if (preferences.Status == DataStatus.Corrupt)
            {
                Debug.LogWarning($"[GameFlow] Preferences were unreadable and were reset: {preferences.Error}");
            }

            ApplyAudio(_preferences.Current);

            try
            {
                await _game.LoadProgressAsync(cancellation);
            }
            catch (InvalidDataException ex)
            {
                Debug.LogError("[GameFlow] The saved progress is corrupt and was kept on disk, so nothing will be saved over it: " + ex.Message);
            }
        }

        public void Dispose()
        {
            _preferences.Changed -= ApplyAudio;
        }

        private void ApplyAudio(PlayerPreferences preferences)
        {
            AudioPreferences audio = preferences.audio;
            _audio.SetVolume(AudioChannel.Master, audio.muted ? 0f : audio.master);
            _audio.SetVolume(AudioChannel.Music, audio.music);
            _audio.SetVolume(AudioChannel.Sfx, audio.sfx);
            _audio.SetVolume(AudioChannel.UI, audio.ui);
        }
    }
}
