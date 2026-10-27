using System;
using UnityEngine;

namespace Pretzel.Preferences
{
    /// <summary>Volumes from 0 to 1. Forward them to your audio system when preferences change.</summary>
    [Serializable]
    public sealed class AudioPreferences
    {
        [Range(0f, 1f)] public float master = 1f;
        [Range(0f, 1f)] public float music = 0.8f;
        [Range(0f, 1f)] public float sfx = 1f;
        [Range(0f, 1f)] public float ui = 1f;
        [Range(0f, 1f)] public float voice = 1f;
        public bool muted;
        public bool subtitles = true;

        public AudioPreferences Clone() => (AudioPreferences)MemberwiseClone();
    }
}
