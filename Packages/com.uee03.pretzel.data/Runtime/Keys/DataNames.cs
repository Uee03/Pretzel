using System;
using System.IO;

namespace Pretzel.Data
{
    /// <summary>
    /// Validates file and folder names so a key can never escape the store root.
    /// </summary>
    internal static class DataNames
    {
        public const int MaxLength = 128;

        private static readonly char[] ForbiddenChars = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

        private static readonly string[] ReservedNames =
        {
            "CON", "PRN", "AUX", "NUL",
            "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        /// <summary>Returns null when valid, otherwise the reason.</summary>
        public static string ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Name cannot be null, empty or whitespace.";
            }

            if (name.Length > MaxLength)
            {
                return $"Name cannot be longer than {MaxLength} characters.";
            }

            if (name == "." || name == "..")
            {
                return "Name cannot be '.' or '..'.";
            }

            if (name[0] == ' ' || name[name.Length - 1] == ' ' || name[name.Length - 1] == '.')
            {
                return "Name cannot start or end with a space, or end with a dot.";
            }

            if (name.IndexOfAny(ForbiddenChars) >= 0 || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return "Name contains a path separator or an invalid character.";
            }

            foreach (char c in name)
            {
                if (char.IsControl(c))
                {
                    return "Name contains a control character.";
                }
            }

            int dot = name.IndexOf('.');
            string stem = dot < 0 ? name : name.Substring(0, dot);
            if (Array.Exists(ReservedNames, reserved => string.Equals(reserved, stem, StringComparison.OrdinalIgnoreCase)))
            {
                return $"'{stem}' is a reserved device name.";
            }

            return null;
        }

        /// <summary>Validates a relative folder path made of '/' separated segments. Returns null when valid.</summary>
        public static string ValidateFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder))
            {
                return null;
            }

            foreach (string segment in folder.Split('/'))
            {
                string error = ValidateName(segment);
                if (error != null)
                {
                    return $"Folder '{folder}': {error}";
                }
            }

            return null;
        }
    }
}
