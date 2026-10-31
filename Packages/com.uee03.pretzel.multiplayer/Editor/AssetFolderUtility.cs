using System.IO;
using UnityEditor;

namespace Pretzel.Multiplayer.Editor
{
    /// <summary>Creates nested asset folders through AssetDatabase so the .meta files are generated.</summary>
    public static class AssetFolderUtility
    {
        public static void EnsureFolder(string assetPath)
        {
            string normalized = assetPath.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            string parent = Path.GetDirectoryName(normalized).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(normalized));
        }
    }
}
