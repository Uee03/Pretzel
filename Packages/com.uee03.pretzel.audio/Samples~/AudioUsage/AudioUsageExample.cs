using Pretzel.Audio;
using UnityEngine;

namespace Pretzel.Samples.Audio
{
    /// <summary>Plays cues, switches music and changes channel volumes from the context menu.</summary>
    public sealed class AudioUsageExample : MonoBehaviour
    {
        [SerializeField] private AudioManager _audio;
        [SerializeField] private AudioCue _footstep;
        [SerializeField] private AudioCue _explosion;
        [SerializeField] private AudioClip _calmMusic;
        [SerializeField] private AudioClip _battleMusic;

        private AudioHandle _lastExplosion;

        private void Start()
        {
            if (!_audio)
            {
                Debug.LogError("Assign an AudioManager.", this);
                enabled = false;
                return;
            }

            _audio.PlayMusic(_calmMusic, 0f);
        }

        [ContextMenu("Footstep")]
        public void Footstep() => _audio.Play(_footstep);

        [ContextMenu("Explosion Here")]
        public void Explosion() => _lastExplosion = _audio.Play(_explosion, transform.position);

        [ContextMenu("Stop Last Explosion")]
        public void StopExplosion() => _audio.Stop(_lastExplosion);

        [ContextMenu("Battle Music")]
        public void Battle() => _audio.PlayMusic(_battleMusic, 1.5f);

        [ContextMenu("Calm Music")]
        public void Calm() => _audio.PlayMusic(_calmMusic, 3f);

        [ContextMenu("Music 30 %")]
        public void QuietMusic() => _audio.SetVolume(AudioChannel.Music, 0.3f);

        [ContextMenu("Mute Master")]
        public void Mute() => _audio.SetVolume(AudioChannel.Master, 0f);

        [ContextMenu("Master 100 %")]
        public void Unmute() => _audio.SetVolume(AudioChannel.Master, 1f);
    }
}
