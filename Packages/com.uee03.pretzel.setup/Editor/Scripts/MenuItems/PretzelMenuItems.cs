using System.IO;
using Pretzel.Setup.Folders;
using Pretzel.Setup.Paths;
using Pretzel.Setup.Settings;
using UnityEditor;
using UnityEngine;

namespace Pretzel.Setup.MenuItems
{
    public static class PretzelMenuItems
    {
        private const string OpenRoot = "Pretzel/Open/";

        [MenuItem("Pretzel/Create/Folders", priority = 120)]
        private static void CreateFolders()
        {
            FolderPreset preset = PretzelSettings.Project.ActivePreset;
            int created = ProjectFolders.Create(preset.folders);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Pretzel] Folder preset '{preset.name}': created {created} folder(s).");
        }

        [MenuItem(OpenRoot + "Application Data Path", priority = 200)]
        private static void OpenDataPath() => Reveal(Application.dataPath);

        [MenuItem(OpenRoot + "Persistent Data Path", priority = 201)]
        private static void OpenPersistentPath() => Reveal(Application.persistentDataPath);

        [MenuItem(OpenRoot + "Resources Folder", priority = 202)]
        private static void OpenResourcesFolder()
        {
            string folder = PretzelSettings.User.resourcesFolder;
            if (string.IsNullOrWhiteSpace(folder))
            {
                EditorUtility.DisplayDialog("Resources folder", "No resources folder is set. Set one in Pretzel/Project Setup, on the Settings tab.", "OK");
                return;
            }

            Reveal(folder);
        }

        [MenuItem(OpenRoot + "Temporary Folder", priority = 203)]
        private static void OpenTemporaryFolder()
        {
            PretzelUserSettings user = PretzelSettings.User;
            string folder = user.useCustomTempFolder && !string.IsNullOrWhiteSpace(user.tempFolder)
                ? user.tempFolder
                : PretzelPaths.SystemTempFolder;

            // The default temp folder is created on first use, so there is always something to open.
            if (Path.IsPathRooted(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            Reveal(folder);
        }

        private static void Reveal(string folder)
        {
            if (!PretzelPaths.Open(folder))
            {
                EditorUtility.DisplayDialog("Folder not found", $"'{folder}' does not exist.", "OK");
            }
        }
    }
}
