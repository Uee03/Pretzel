using UnityEngine;

namespace Pretzel.Samples.Extensions
{
    /// <summary>An empty behaviour that the MonoBehaviour extension examples switch on, off and destroy.</summary>
    public sealed class ExtensionsTicker : MonoBehaviour
    {
        private void OnEnable() => Debug.Log($"{name}: ticker enabled at {Time.time:0.0}s.", this);

        private void OnDisable() => Debug.Log($"{name}: ticker disabled at {Time.time:0.0}s.", this);
    }
}
