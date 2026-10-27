using System;
using UnityEngine;

namespace Pretzel.Preferences
{
    [Serializable]
    public sealed class ControlPreferences
    {
        /// <summary>Input System rebinds, from InputActionAsset.SaveBindingOverridesAsJson. Empty means defaults.</summary>
        public string bindingOverridesJson = string.Empty;

        [Range(0.05f, 10f)] public float lookSensitivity = 1f;
        public bool invertY;
        public bool vibration = true;

        public ControlPreferences Clone() => (ControlPreferences)MemberwiseClone();
    }
}
