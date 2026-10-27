using UnityEngine;

namespace Pretzel.Samples.EditorUIHelpers
{
    /// <summary>Settings edited by the sample window. Lives in the Editor assembly because only the window uses it.</summary>
    public sealed class ExportSettings : ScriptableObject
    {
        public string outputFolder = "Assets/Exports";
        public string fileName = "export";
        [Range(1, 8)] public int scale = 1;
        public bool includeHidden;
        [Tooltip("Shown only when Include Hidden is on.")]
        public string hiddenPrefix = "_";
    }
}
