using Pretzel.Setup.Settings;
using UnityEditor;
using UnityEngine;

namespace Pretzel.Setup.Windows
{
    /// <summary>Opens the Setup window on the Main tab the first time Pretzel is loaded for this user.</summary>
    [InitializeOnLoad]
    internal static class PretzelFirstRun
    {
        static PretzelFirstRun()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            // The editor is not ready to open windows during a domain reload.
            EditorApplication.delayCall += OpenOnce;
        }

        private static void OpenOnce()
        {
            PretzelUserSettings user = PretzelSettings.User;
            if (user.firstRunShown)
            {
                return;
            }

            user.firstRunShown = true;
            user.lastTab = 0;
            PretzelSettings.SaveUser();
            PretzelSetupWindow.Open();
        }
    }
}
