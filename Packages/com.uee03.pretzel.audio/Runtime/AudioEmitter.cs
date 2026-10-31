using UnityEngine;

namespace Pretzel.Audio
{
    /// <summary>One pooled AudioSource and the state of the sound it currently plays.</summary>
    internal sealed class AudioEmitter
    {
        public int Id;
        public int Version;
        public AudioSource Source;
        public AudioChannel Channel;
        public float BaseVolume;
    }
}
