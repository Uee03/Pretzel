using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Pretzel.Setup.Folders
{
    /// <summary>Creates folders under Assets from relative paths such as "_Project/Audio/SFX".</summary>
    public static class ProjectFolders
    {
        public static string Normalize(string folder)
        {
            return (folder ?? string.Empty).Trim().Replace('\\', '/').Trim('/');
        }

        /// <summary>A folder is valid when it is relative, stays inside Assets, and every segment is a legal name.</summary>
        public static bool IsValid(string folder, out string error)
        {
            string normalized = Normalize(folder);
            if (normalized.Length == 0)
            {
                error = "The path is empty.";
                return false;
            }

            if (Path.IsPathRooted(normalized))
            {
                error = "The path must be relative to Assets.";
                return false;
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (string segment in normalized.Split('/'))
            {
                if (segment.Length == 0 || segment == "." || segment == ".." || segment.IndexOfAny(invalid) >= 0 || segment.EndsWith(".", StringComparison.Ordinal))
                {
                    error = $"'{segment}' is not a valid folder name.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public static bool Exists(string folder)
        {
            return IsValid(folder, out _) && AssetDatabase.IsValidFolder("Assets/" + Normalize(folder));
        }

        /// <summary>Creates every missing folder. Invalid entries are skipped with a warning. Returns how many folders were created.</summary>
        public static int Create(IEnumerable<string> folders)
        {
            int created = 0;
            foreach (string folder in folders)
            {
                if (!IsValid(folder, out string error))
                {
                    Debug.LogWarning($"[Pretzel] Skipped folder '{folder}': {error}");
                    continue;
                }

                string parent = "Assets";
                foreach (string segment in Normalize(folder).Split('/'))
                {
                    string path = parent + "/" + segment;
                    if (!AssetDatabase.IsValidFolder(path))
                    {
                        AssetDatabase.CreateFolder(parent, segment);
                        created++;
                    }

                    parent = path;
                }
            }

            return created;
        }
    }
}
