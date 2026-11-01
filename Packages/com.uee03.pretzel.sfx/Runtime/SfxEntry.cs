using System;
using Pretzel.Audio;
using UnityEngine;

namespace Pretzel.Sfx
{
    /// <summary>One named sound: the cue to play and how often it may play.</summary>
    [Serializable]
    public sealed class SfxEntry
    {
        [SerializeField] private string _id;
        [SerializeField] private AudioCue _cue;

        [Tooltip("Minimum seconds between two plays of this sound. 0 allows every call. Stops a sound that fires every frame from becoming a buzz.")]
        [SerializeField, Min(0f)] private float _cooldownSeconds;

        [Tooltip("The most voices of this sound that may play at once. 0 means no limit.")]
        [SerializeField, Min(0)] private int _maxConcurrent;

        public SfxEntry()
        {
        }

        public SfxEntry(string id, AudioCue cue, float cooldownSeconds = 0f, int maxConcurrent = 0)
        {
            _id = id;
            _cue = cue;
            _cooldownSeconds = Mathf.Max(0f, cooldownSeconds);
            _maxConcurrent = Mathf.Max(0, maxConcurrent);
        }

        public string Id => _id;

        public AudioCue Cue => _cue;

        public float CooldownSeconds => _cooldownSeconds;

        public int MaxConcurrent => _maxConcurrent;
    }
}
