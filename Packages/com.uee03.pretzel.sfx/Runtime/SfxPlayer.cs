using System;
using System.Collections.Generic;
using Pretzel.Audio;
using UnityEngine;

namespace Pretzel.Sfx
{
    /// <summary>
    /// Plays the sounds of an <see cref="SfxLibrary"/> through an <see cref="IAudioService"/>. A sound that is asked for
    /// too often, or while too many of its voices still play, is skipped instead of piling up. It allocates nothing
    /// once a sound has played, and it tells you once about an id that is not in the library.
    /// </summary>
    public sealed class SfxPlayer : ISfxPlayer
    {
        private readonly IAudioService _audio;
        private readonly SfxLibrary _library;
        private readonly Func<float> _clock;
        private readonly SfxThrottle _throttle = new();
        private readonly Dictionary<string, List<AudioHandle>> _voices = new(StringComparer.Ordinal);
        private readonly HashSet<string> _reported = new(StringComparer.Ordinal);

        /// <param name="clock">Seconds, for cooldowns. Defaults to unscaled time, so a sound is not blocked by hit stop.</param>
        public SfxPlayer(IAudioService audio, SfxLibrary library, Func<float> clock = null)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _library = library ?? throw new ArgumentNullException(nameof(library));
            _clock = clock ?? (() => Time.unscaledTime);
        }

        /// <summary>How many calls reached the audio service.</summary>
        public int Played { get; private set; }

        /// <summary>How many calls were skipped by a cooldown or a voice limit.</summary>
        public int Suppressed { get; private set; }

        public AudioHandle Play(string id)
        {
            return PlayInternal(id, null);
        }

        public AudioHandle Play(string id, Vector3 position)
        {
            return PlayInternal(id, position);
        }

        public void Stop(string id)
        {
            if (id != null && _voices.TryGetValue(id, out List<AudioHandle> voices))
            {
                foreach (AudioHandle handle in voices)
                {
                    _audio.Stop(handle);
                }

                voices.Clear();
            }
        }

        private AudioHandle PlayInternal(string id, Vector3? position)
        {
            if (!_library.TryGet(id, out SfxEntry entry) || entry.Cue == null || !entry.Cue.HasClips)
            {
                ReportOnce(id);
                return AudioHandle.None;
            }

            if (entry.MaxConcurrent > 0 && ActiveVoices(id) >= entry.MaxConcurrent)
            {
                Suppressed++;
                return AudioHandle.None;
            }

            if (!_throttle.TryAcquire(id, _clock(), entry.CooldownSeconds))
            {
                Suppressed++;
                return AudioHandle.None;
            }

            AudioHandle handle = position.HasValue ? _audio.Play(entry.Cue, position.Value) : _audio.Play(entry.Cue);
            Played++;
            if (entry.MaxConcurrent > 0 && handle.IsValid)
            {
                VoicesOf(id).Add(handle);
            }

            return handle;
        }

        private int ActiveVoices(string id)
        {
            if (!_voices.TryGetValue(id, out List<AudioHandle> voices))
            {
                return 0;
            }

            for (int i = voices.Count - 1; i >= 0; i--)
            {
                if (!_audio.IsPlaying(voices[i]))
                {
                    voices.RemoveAt(i);
                }
            }

            return voices.Count;
        }

        private List<AudioHandle> VoicesOf(string id)
        {
            if (!_voices.TryGetValue(id, out List<AudioHandle> voices))
            {
                voices = new List<AudioHandle>(4);
                _voices[id] = voices;
            }

            return voices;
        }

        private void ReportOnce(string id)
        {
            if (_reported.Add(id ?? string.Empty))
            {
                Debug.LogWarning($"[SFX] '{id}' is not in the library, or its cue has no clips. It will not play.");
            }
        }
    }
}
