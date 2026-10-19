using System;
using System.Collections.Generic;

namespace Pretzel.Setup.Settings
{
    [Serializable]
    public sealed class PinnedVersion
    {
        public string id;
        public string version;
    }

    /// <summary>Project-wide Pretzel settings. Stored in ProjectSettings/Pretzel/pretzel-settings.json and committed.</summary>
    [Serializable]
    public sealed class PretzelProjectSettings
    {
        public List<FolderPreset> folderPresets = FolderPreset.CreateDefaults();
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
                FolderPreset preset = folderPresets.Find(p => p.name == activeFolderPreset);
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
                FolderPreset existing = folderPresets.Find(p => p.name == fallback.name);
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

            if (!folderPresets.Exists(p => p.name == activeFolderPreset))
            {
                activeFolderPreset = FolderPreset.Game;
            }

            settingsFolder ??= string.Empty;
            moduleSourceUrl ??= string.Empty;
            moduleSourceVersion ??= string.Empty;
        }
    }

    /// <summary>Per-user Pretzel settings. Stored in UserSettings/Pretzel/pretzel-user.json and normally not committed.</summary>
    [Serializable]
    public sealed class PretzelUserSettings
    {
        public string resourcesFolder = string.Empty;
        public bool useCustomTempFolder;
        public string tempFolder = string.Empty;
        public int lastTab;

        // The Setup window opens by itself once. The starter kit offer stays on the Main tab until it is used or skipped.
        public bool firstRunShown;
        public bool starterKitDone;
    }
}
