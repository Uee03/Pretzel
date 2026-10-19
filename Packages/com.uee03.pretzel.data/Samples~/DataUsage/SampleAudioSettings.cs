using UnityEngine;

namespace Pretzel.Samples.Data
{
    [CreateAssetMenu(menuName = "Pretzel/Samples/Audio Settings")]
    public class SampleAudioSettings : ScriptableObject
    {
        public float masterVolume = 1f;
        public float musicVolume = 0.8f;
    }
}
