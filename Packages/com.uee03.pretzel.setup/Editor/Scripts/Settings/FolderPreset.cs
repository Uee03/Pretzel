using System;
using System.Collections.Generic;

namespace Pretzel.Setup.Settings
{
    [Serializable]
    public sealed class FolderPreset
    {
        public const string Game = "Game";
        public const string Tool = "Tool";
        public const string Systems = "System";
        public const string Custom = "Custom";

        // Every script lives under _Scripts. Everything else lives under _Project.
        private const string Scripts = "_Scripts";
        private const string Project = "_Project";

        public string name;
        public List<string> folders = new();

        public FolderPreset()
        {
        }

        public FolderPreset(string name, params string[] folders)
        {
            this.name = name;
            this.folders = new List<string>(folders);
        }

        public static List<FolderPreset> CreateDefaults()
        {
            string[] scripts = { Scripts + "/Editor", Scripts + "/Runtime" };

            return new List<FolderPreset>
            {
                Create(Game, scripts,
                    "Animations", "Art/Materials", "Art/Models", "Art/Shaders", "Art/Sprites", "Art/Textures",
                    "Audio/BGM", "Audio/SFX", "Prefabs", "Scenes", "ScriptableObjects", "Settings", "UI/Fonts", "UI/Icons"),
                Create(Tool, scripts,
                    "Documentation", "Prefabs", "Samples", "Scenes", "ScriptableObjects", "Settings", "UI/Icons", "UI/Layouts", "UI/Styles"),
                Create(Systems, scripts,
                    "Documentation", "Prefabs", "Samples", "Scenes", "ScriptableObjects", "Settings"),
                Create(Custom, scripts, "Prefabs", "Scenes", "ScriptableObjects")
            };
        }

        /// <summary>Rewrites the old _Project/Scripts/... paths to _Scripts/... . Returns true when anything changed.</summary>
        public bool MigrateScriptsFolder()
        {
            bool changed = false;
            for (int i = 0; i < folders.Count; i++)
            {
                string folder = folders[i];
                if (folder == null)
                {
                    continue;
                }

                string normalized = folder.Trim().Replace('\\', '/').Trim('/');
                const string old = Project + "/Scripts";
                if (normalized == old || normalized.StartsWith(old + "/", StringComparison.Ordinal))
                {
                    folders[i] = Scripts + normalized.Substring(old.Length);
                    changed = true;
                }
            }

            return changed;
        }

        private static FolderPreset Create(string name, string[] scripts, params string[] projectFolders)
        {
            List<string> list = new(scripts);
            foreach (string folder in projectFolders)
            {
                list.Add(Project + "/" + folder);
            }

            return new FolderPreset { name = name, folders = list };
        }
    }
}
