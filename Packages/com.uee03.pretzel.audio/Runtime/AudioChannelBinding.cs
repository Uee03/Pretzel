using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Pretzel.Audio
{
    /// <summary>Routes a channel to a mixer group, and optionally to an exposed mixer volume parameter.</summary>
    [Serializable]
    public sealed class AudioChannelBinding
    {
        public AudioChannel channel;
        public AudioMixerGroup group;

        [Tooltip("Exposed AudioMixer parameter for this channel's volume, in decibels. Leave empty to scale AudioSource volume instead.")]
        public string volumeParameter;
    }
}
