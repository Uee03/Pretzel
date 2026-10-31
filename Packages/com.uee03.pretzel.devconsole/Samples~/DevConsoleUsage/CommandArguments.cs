using System;
using System.Globalization;

namespace Pretzel.Samples.Console
{
    /// <summary>Argument parsing for console commands. Culture-invariant, so "1.5" works on every machine.</summary>
    public static class CommandArguments
    {
        public static bool TryFloat(string[] args, int index, out float value)
        {
            value = 0f;
            return index < args.Length && float.TryParse(args[index], NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        public static bool TryInt(string[] args, int index, out int value)
        {
            value = 0;
            return index < args.Length && int.TryParse(args[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>No argument flips <paramref name="current"/>; "on"/"off", "true"/"false" and "1"/"0" set it.</summary>
        public static bool TryToggle(string[] args, bool current, out bool value)
        {
            value = !current;
            if (args.Length == 0)
            {
                return true;
            }

            if (Matches(args[0], "on", "true", "1"))
            {
                value = true;
                return true;
            }

            if (Matches(args[0], "off", "false", "0"))
            {
                value = false;
                return true;
            }

            return false;
        }

        private static bool Matches(string text, params string[] options)
        {
            foreach (string option in options)
            {
                if (string.Equals(text, option, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
