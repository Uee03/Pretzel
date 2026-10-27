using UnityEngine;

namespace Pretzel.Audio
{
    /// <summary>A playable sound: one or more clips with volume and pitch ranges, a channel and 3D settings.</summary>
    [CreateAssetMenu(menuName = "Pretzel/Audio/Audio Cue", fileName = "AudioCue")]
    public sealed class AudioCue : ScriptableObject
    {
        [SerializeField] private AudioClip[] _clips = new AudioClip[0];
        [SerializeField] private AudioChannel _channel = AudioChannel.Sfx;
        [SerializeField] private Vector2 _volume = new(1f, 1f);
        [SerializeField] private Vector2 _pitch = new(1f, 1f);
        [SerializeField] private bool _loop;

        [Tooltip("Never plays the same clip twice in a row when there are several.")]
        [SerializeField] private bool _avoidRepeat = true;

        [Header("3D")]
        [SerializeField, Range(0f, 1f)] private float _spatialBlend;
        [SerializeField, Min(0f)] private float _minDistance = 1f;
        [SerializeField, Min(0f)] private float _maxDistance = 50f;

        // Runtime state only, never serialized.
        [System.NonSerialized] private int _lastIndex = -1;

        public AudioChannel Channel => _channel;

        public bool Loop => _loop;

        public float SpatialBlend => _spatialBlend;

        public float MinDistance => _minDistance;

        public float MaxDistance => _maxDistance;

        public bool HasClips => _clips != null && _clips.Length > 0;

        public AudioClip PickClip()
        {
            if (!HasClips)
            {
                return null;
            }

            if (_clips.Length == 1)
            {
                return _clips[0];
            }

            int index = Random.Range(0, _clips.Length);
            if (_avoidRepeat && index == _lastIndex)
            {
                index = (index + 1 + Random.Range(0, _clips.Length - 1)) % _clips.Length;
            }

            _lastIndex = index;
            return _clips[index];
        }

        public float PickVolume() => Random.Range(Mathf.Min(_volume.x, _volume.y), Mathf.Max(_volume.x, _volume.y));

        public float PickPitch() => Random.Range(Mathf.Min(_pitch.x, _pitch.y), Mathf.Max(_pitch.x, _pitch.y));

        /// <summary>Sets the clips from code, for tests and generated content.</summary>
        public void SetClips(params AudioClip[] clips)
        {
            _clips = clips ?? new AudioClip[0];
            _lastIndex = -1;
        }
    }
}
