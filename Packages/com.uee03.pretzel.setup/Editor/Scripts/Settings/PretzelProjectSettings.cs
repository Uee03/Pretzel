using System;
using System.Collections.Generic;

namespace Pretzel.Setup.Settings
{
    /// <summary>Project-wide Pretzel settings. Stored in ProjectSettings/Pretzel/pretzel-settings.json and committed.</summary>
    [Serializable]
    public sealed class PretzelProjectSettings
    {
        public List<FolderPreset> folderPresets = new(FolderPreset.CreateDefaults());
        public string activeFolderPreset = FolderPreset.Game;

        // The serializer, save folder and file extension live in the DataIOConfig asset, edited in Pretzel/Data Format.
        public string settingsFolder = "Settings";

        public List<string> catalogSources = new();
        public List<PinnedVersion> pinnedVersions = new();

        // Where Pretzel modules are installed from, for example https://github.com/owner/Pretzel.git. Empty disables install.
        public string moduleSourceUrl = string.Empty;
        public string moduleSourceVersion = string.Empty;

        public FolderPreset ActivePreset
        {
            get
            {
                FolderPreset preset = folderPresets.Find(p => string.Equals(p.name, activeFolderPreset, StringComparison.OrdinalIgnoreCase));
                return preset ?? folderPresets[0];
            }
        }

        /// <summary>Repairs a loaded file: the default presets must exist, and the active one must be valid.</summary>
        public void Normalize()
        {
            folderPresets ??= new List<FolderPreset>();
            catalogSources ??= new List<string>();
            pinnedVersions ??= new List<PinnedVersion>();

            foreach (FolderPreset fallback in FolderPreset.CreateDefaults())
            {
                FolderPreset existing = folderPresets.Find(p => string.Equals(p.name, fallback.name, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    folderPresets.Add(fallback);
                }
                else
                {
                    existing.folders ??= new List<string>();
                }
            }

            foreach (FolderPreset preset in folderPresets)
            {
                preset.folders ??= new List<string>();
                preset.MigrateScriptsFolder();
            }

            if (!folderPresets.Exists(p => string.Equals(p.name, activeFolderPreset, StringComparison.OrdinalIgnoreCase)))
            {
                activeFolderPreset = FolderPreset.Game;
            }

            settingsFolder ??= string.Empty;
            moduleSourceUrl ??= string.Empty;
            moduleSourceVersion ??= string.Empty;
        }
    }

}
