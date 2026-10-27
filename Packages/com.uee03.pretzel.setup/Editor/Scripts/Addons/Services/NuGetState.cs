using System;
using System.IO;
using System.Text.RegularExpressions;
using Pretzel.Setup.Paths;
using UnityEditor;

namespace Pretzel.Setup.Addons.Services
{
    /// <summary>Reads what NuGetForUnity has installed. Pretzel cannot install NuGet packages itself.</summary>
    public static class NuGetState
    {
        private const string MenuPath = "NuGet/Manage NuGet Packages";

        private static readonly string[] _configPaths =
        {
            "Assets/packages.config",
            "Packages/nuget-packages/packages.config"
        };

        public static bool IsInstalled(string nugetId)
        {
            if (string.IsNullOrEmpty(nugetId))
            {
                return false;
            }

            Regex pattern = new($"<package\\s+id=\"{Regex.Escape(nugetId)}\"", RegexOptions.IgnoreCase);
            foreach (string relative in _configPaths)
            {
                string path = Path.Combine(PretzelPaths.ProjectFolder, relative);
                if (File.Exists(path) && pattern.IsMatch(File.ReadAllText(path)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Opens the NuGetForUnity window. Returns false when NuGetForUnity is not installed.</summary>
        public static bool OpenWindow() => EditorApplication.ExecuteMenuItem(MenuPath);
    }
}
