using System;

namespace Pretzel.Setup.Settings
{
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
