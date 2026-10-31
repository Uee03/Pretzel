using UnityEditor;
using UnityEngine;

namespace Pretzel.Samples.UI.EditorWindows
{
    /// <summary>Per-user, per-project settings. The file lives in UserSettings/, which version control normally ignores.</summary>
    [FilePath("UserSettings/Pretzel/Samples/BasicsSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class BasicsSettings : ScriptableSingleton<BasicsSettings>
    {
        public const string VolumePath = nameof(_volume);
        public const string HighlightPath = nameof(_highlight);
        public const string NotePath = nameof(_note);

        [SerializeField, Range(0f, 1f)] private float _volume = 0.5f;
        [SerializeField] private bool _highlight;
        [SerializeField] private string _note = "Edit me";

        public static string StoragePath => GetFilePath();

        public float Volume => _volume;

        public bool Highlight => _highlight;

        public string Note => _note;

        // Save is protected on ScriptableSingleton, so the window calls this.
        public void SaveSettings()
        {
            Save(true);
        }
    }
}
