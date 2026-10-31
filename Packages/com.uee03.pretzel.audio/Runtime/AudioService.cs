using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Audio;

namespace Pretzel.Audio
{
    /// <summary>
    /// Plays sounds on a fixed budget of pooled AudioSources and crossfades music on two dedicated sources.
    /// A channel with an exposed mixer parameter is controlled through the <see cref="AudioMixer"/>; any other channel scales
    /// AudioSource volume directly. When the budget is used up, the oldest non-looping sound is stolen.
    /// </summary>
    [AddComponentMenu("Pretzel/Audio/Audio Service")]
    public sealed class AudioService : MonoBehaviour, IAudioService
    {
        private static readonly int _channelCount = Enum.GetValues(typeof(AudioChannel)).Length;

        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private List<AudioChannelBinding> _channels = new();
        [SerializeField, Min(1)] private int _maxVoices = 32;
        [SerializeField, Min(0)] private int _prewarmVoices = 8;
        [SerializeField, Min(0f)] private float _defaultMusicFade = 1f;

        private readonly List<AudioEmitter> _emitters = new();
        private readonly Stack<AudioEmitter> _idle = new();
        private readonly List<AudioEmitter> _playing = new();
        private float[] _volumes;
        private AudioChannelBinding[] _bindings;
        private AudioSource _musicA;
        private AudioSource _musicB;
        private bool _musicOnA;
        private float _musicVolume = 1f;
        private CancellationTokenSource _musicFade;
        private bool _musicFading;
        private int _nextVersion;

        public AudioClip CurrentMusic => CurrentMusicSource && CurrentMusicSource.isPlaying ? CurrentMusicSource.clip : null;

        private AudioSource CurrentMusicSource => _musicOnA ? _musicA : _musicB;

        private void Awake()
        {
            _volumes = new float[_channelCount];
            for (int i = 0; i < _channelCount; i++)
            {
                _volumes[i] = 1f;
            }

            _bindings = new AudioChannelBinding[_channelCount];
            foreach (AudioChannelBinding binding in _channels)
            {
                if (binding != null)
                {
                    _bindings[(int)binding.channel] = binding;
                }
            }

            _musicA = CreateSource("Music A", AudioChannel.Music);
            _musicB = CreateSource("Music B", AudioChannel.Music);
            _musicA.loop = _musicB.loop = true;
            for (int i = 0; i < Mathf.Min(_prewarmVoices, _maxVoices); i++)
            {
                _idle.Push(CreateEmitter());
            }
        }

        // AudioMixer.SetFloat does not stick when called in Awake.
        private void Start()
        {
            for (int i = 0; i < _channelCount; i++)
            {
                ApplyMixerVolume((AudioChannel)i);
            }
        }

        private void Update()
        {
            for (int i = _playing.Count - 1; i >= 0; i--)
            {
                if (!_playing[i].Source.isPlaying)
                {
                    Recycle(i);
                }
            }
        }

        private void OnDestroy()
        {
            _musicFade?.Cancel();
            _musicFade?.Dispose();
            _musicFade = null;
        }

        #region SOUNDS

        public AudioHandle Play(AudioCue cue) => PlayCue(cue, null);

        public AudioHandle Play(AudioCue cue, Vector3 position) => PlayCue(cue, position);

        public AudioHandle PlayClip(AudioClip clip, AudioChannel channel = AudioChannel.Sfx, float volume = 1f)
        {
            if (!clip)
            {
                return AudioHandle.None;
            }

            AudioEmitter emitter = Rent();
            if (emitter == null)
            {
                return AudioHandle.None;
            }

            AudioSource source = emitter.Source;
            source.transform.position = transform.position;
            source.clip = clip;
            source.loop = false;
            source.pitch = 1f;
            source.spatialBlend = 0f;
            return Begin(emitter, channel, volume);
        }

        public bool IsPlaying(AudioHandle handle) => TryGet(handle, out AudioEmitter emitter) && emitter.Source.isPlaying;

        public void Stop(AudioHandle handle)
        {
            if (TryGet(handle, out AudioEmitter emitter))
            {
                Recycle(_playing.IndexOf(emitter));
            }
        }

        public void StopAll()
        {
            for (int i = _playing.Count - 1; i >= 0; i--)
            {
                Recycle(i);
            }
        }

        private AudioHandle PlayCue(AudioCue cue, Vector3? position)
        {
            if (!cue)
            {
                return AudioHandle.None;
            }

            AudioClip clip = cue.PickClip();
            if (!clip)
            {
                return AudioHandle.None;
            }

            AudioEmitter emitter = Rent();
            if (emitter == null)
            {
                return AudioHandle.None;
            }

            AudioSource source = emitter.Source;
            source.transform.position = position ?? transform.position;
            source.clip = clip;
            source.loop = cue.Loop;
            source.pitch = cue.PickPitch();
            source.spatialBlend = cue.SpatialBlend;
            source.minDistance = cue.MinDistance;
            source.maxDistance = cue.MaxDistance;
            return Begin(emitter, cue.Channel, cue.PickVolume());
        }

        private AudioHandle Begin(AudioEmitter emitter, AudioChannel channel, float volume)
        {
            emitter.Channel = channel;
            emitter.BaseVolume = volume;
            emitter.Version = ++_nextVersion;
            emitter.Source.outputAudioMixerGroup = GroupOf(channel);
            emitter.Source.volume = volume * ChannelScale(channel);
            emitter.Source.Play();
            _playing.Add(emitter);
            return new AudioHandle(emitter.Id, emitter.Version);
        }

        private AudioEmitter Rent()
        {
            if (_idle.Count > 0)
            {
                return _idle.Pop();
            }

            if (_emitters.Count < _maxVoices)
            {
                return CreateEmitter();
            }

            for (int i = 0; i < _playing.Count; i++)
            {
                if (!_playing[i].Source.loop)
                {
                    Recycle(i);
                    return _idle.Pop();
                }
            }

            return null;
        }

        private void Recycle(int playingIndex)
        {
            if (playingIndex < 0)
            {
                return;
            }

            AudioEmitter emitter = _playing[playingIndex];
            _playing.RemoveAt(playingIndex);
            emitter.Source.Stop();
            emitter.Source.clip = null;
            emitter.Version = 0;
            _idle.Push(emitter);
        }

        private bool TryGet(AudioHandle handle, out AudioEmitter emitter)
        {
            emitter = handle.IsValid && handle.Id >= 0 && handle.Id < _emitters.Count ? _emitters[handle.Id] : null;
            return emitter != null && emitter.Version == handle.Version;
        }

        private AudioEmitter CreateEmitter()
        {
            AudioEmitter emitter = new() { Id = _emitters.Count, Source = CreateSource("Emitter " + _emitters.Count, AudioChannel.Sfx) };
            _emitters.Add(emitter);
            return emitter;
        }

        private AudioSource CreateSource(string sourceName, AudioChannel channel)
        {
            GameObject child = new(sourceName);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = GroupOf(channel);
            return source;
        }

        #endregion

        #region MUSIC

        /// <summary>Crossfades to a clip. Playing the clip that already plays only updates its volume.</summary>
        /// <param name="fadeSeconds">Unscaled seconds. Negative uses the default fade.</param>
        public void PlayMusic(AudioClip clip, float fadeSeconds = -1f, float volume = 1f)
        {
            if (!clip)
            {
                StopMusic(fadeSeconds);
                return;
            }

            if (CurrentMusic == clip)
            {
                _musicVolume = Mathf.Clamp01(volume);
                if (!_musicFading)
                {
                    CurrentMusicSource.volume = MusicTarget;
                }

                return;
            }

            AudioSource from = CurrentMusicSource;
            _musicOnA = !_musicOnA;
            AudioSource to = CurrentMusicSource;
            _musicVolume = Mathf.Clamp01(volume);
            to.clip = clip;
            to.volume = 0f;
            to.Play();
            Crossfade(from, to, fadeSeconds);
        }

        public void StopMusic(float fadeSeconds = -1f) => Crossfade(CurrentMusicSource, null, fadeSeconds);

        private void Crossfade(AudioSource from, AudioSource to, float fadeSeconds)
        {
            _musicFade?.Cancel();
            _musicFade?.Dispose();
            _musicFade = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

            // A cancelled fade never stops its outgoing source, so stop it here.
            AudioSource other = from == _musicA ? _musicB : _musicA;
            if (other != to && other.isPlaying)
            {
                other.Stop();
                other.volume = 0f;
            }

            _musicFading = true;
            _ = CrossfadeAsync(from, to, fadeSeconds < 0f ? _defaultMusicFade : fadeSeconds, _musicFade.Token);
        }

        private async Awaitable CrossfadeAsync(AudioSource from, AudioSource to, float duration, CancellationToken cancellationToken)
        {
            float fromStart = from ? from.volume : 0f;
            try
            {
                for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
                {
                    float t = elapsed / duration;
                    if (to)
                    {
                        to.volume = Mathf.Lerp(0f, MusicTarget, t);
                    }

                    if (from)
                    {
                        from.volume = Mathf.Lerp(fromStart, 0f, t);
                    }

                    await Awaitable.NextFrameAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (to)
            {
                to.volume = MusicTarget;
            }

            if (from && from != to)
            {
                from.Stop();
                from.volume = 0f;
            }

            _musicFading = false;
        }

        private float MusicTarget => _musicVolume * ChannelScale(AudioChannel.Music);

        #endregion

        #region VOLUME

        /// <param name="linear">0 to 1. Converted to decibels for a mixer parameter.</param>
        public void SetVolume(AudioChannel channel, float linear)
        {
            _volumes[(int)channel] = Mathf.Clamp01(linear);
            ApplyMixerVolume(channel);
            foreach (AudioEmitter emitter in _playing)
            {
                emitter.Source.volume = emitter.BaseVolume * ChannelScale(emitter.Channel);
            }

            if (!_musicFading && CurrentMusicSource && CurrentMusicSource.isPlaying)
            {
                CurrentMusicSource.volume = MusicTarget;
            }
        }

        public float GetVolume(AudioChannel channel) => _volumes[(int)channel];

        private float ChannelScale(AudioChannel channel)
        {
            float scale = UsesMixer(channel) ? 1f : _volumes[(int)channel];
            if (channel != AudioChannel.Master && !UsesMixer(AudioChannel.Master))
            {
                scale *= _volumes[(int)AudioChannel.Master];
            }

            return scale;
        }

        private bool UsesMixer(AudioChannel channel)
        {
            return _mixer && !string.IsNullOrEmpty(_bindings[(int)channel]?.volumeParameter);
        }

        private void ApplyMixerVolume(AudioChannel channel)
        {
            if (UsesMixer(channel) && !_mixer.SetFloat(_bindings[(int)channel].volumeParameter, AudioMath.LinearToDecibels(_volumes[(int)channel])))
            {
                Debug.LogWarning($"AudioMixer '{_mixer.name}' has no exposed parameter '{_bindings[(int)channel].volumeParameter}'.", this);
            }
        }

        private AudioMixerGroup GroupOf(AudioChannel channel) => _bindings?[(int)channel]?.group;

        #endregion
    }
}
