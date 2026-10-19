using System;
using System.IO;

namespace Pretzel.Data.Editor
{
    /// <summary>Checks the relative save folder typed into the Format tab.</summary>
    internal static class SaveFolder
    {
        public static string Normalize(string folder)
        {
            return (folder ?? string.Empty).Trim().Replace('\\', '/').Trim('/');
        }

        /// <summary>A folder is valid when it is relative, stays inside its root, and every segment is a legal name.</summary>
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
                error = "The path must be relative.";
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
    }
}
