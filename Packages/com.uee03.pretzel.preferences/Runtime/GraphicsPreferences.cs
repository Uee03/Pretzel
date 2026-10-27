using System;
using UnityEngine;

namespace Pretzel.Preferences
{
    /// <summary>Display and quality settings. -1 or 0 in a field means "leave the current value alone".</summary>
    [Serializable]
    public sealed class GraphicsPreferences
    {
        /// <summary>Index into QualitySettings.names, or -1.</summary>
        public int qualityLevel = -1;

        /// <summary>Resolution in pixels, or 0 to keep the current one. Applied on desktop platforms only.</summary>
        public int width;
        public int height;
        public FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;

        /// <summary>0 is off, 1 is every vertical blank, -1 keeps the current value.</summary>
        public int vSyncCount = -1;

        /// <summary>Frames per second when vSync is off, or -1 for the platform default.</summary>
        public int targetFrameRate = -1;

        public GraphicsPreferences Clone() => (GraphicsPreferences)MemberwiseClone();

        /// <summary>Reads the current quality, display and frame settings.</summary>
        public static GraphicsPreferences Capture()
        {
            return new GraphicsPreferences
            {
                qualityLevel = QualitySettings.GetQualityLevel(),
                width = Screen.width,
                height = Screen.height,
                fullScreenMode = Screen.fullScreenMode,
                vSyncCount = QualitySettings.vSyncCount,
                targetFrameRate = Application.targetFrameRate
            };
        }

        public void Apply()
        {
            if (qualityLevel >= 0 && qualityLevel < QualitySettings.names.Length && qualityLevel != QualitySettings.GetQualityLevel())
            {
                QualitySettings.SetQualityLevel(qualityLevel, true);
            }

            // A quality level carries its own vSync count, so apply vSync after it.
            if (vSyncCount >= 0)
            {
                QualitySettings.vSyncCount = Mathf.Clamp(vSyncCount, 0, 4);
            }

            Application.targetFrameRate = targetFrameRate;

            if (Application.isMobilePlatform || Application.isConsolePlatform || Application.isEditor)
            {
                return;
            }

            if (width > 0 && height > 0)
            {
                Screen.SetResolution(width, height, fullScreenMode);
            }
            else if (Screen.fullScreenMode != fullScreenMode)
            {
                Screen.fullScreenMode = fullScreenMode;
            }
        }
    }
}
