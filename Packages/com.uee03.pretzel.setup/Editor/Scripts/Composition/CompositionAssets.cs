using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pretzel.Setup.Composition
{
    /// <summary>
    /// Creates the root scope prefab, the installer asset and VContainerSettings, and keeps VContainerSettings in the
    /// preloaded assets so VContainer creates the root scope before the first scene loads.
    /// </summary>
    internal static class CompositionAssets
    {
        public const string VContainerPackage = "jp.hadashikick.vcontainer";

        private const string ScopeTypeName = "Pretzel.Integrations.VContainer.PretzelLifetimeScope, Pretzel.Integrations.VContainer";
        private const string InstallerBaseTypeName = "Pretzel.Integrations.VContainer.ScriptableInstaller, Pretzel.Integrations.VContainer";
        private const string SettingsTypeName = "VContainer.Unity.VContainerSettings, VContainer";
        private const string AudioManagerTypeName = "Pretzel.Audio.AudioManager, Pretzel.Audio";

        public static Type ScopeType => Type.GetType(ScopeTypeName);

        public static Type SettingsType => Type.GetType(SettingsTypeName);

        public static bool IsVContainerInstalled => SettingsType != null;

        public static bool IsIntegrationActive => ScopeType != null;

        public static string ScopePrefabPath(CompositionSettings settings) => $"{settings.assetFolder}/{settings.ScopePrefabName}.prefab";

        public static string InstallerAssetPath(CompositionSettings settings) => $"{settings.assetFolder}/{settings.InstallerName}.asset";

        public static Type FindInstallerType(CompositionSettings settings)
        {
            Type baseType = Type.GetType(InstallerBaseTypeName);
            if (baseType == null)
            {
                return null;
            }

            string fullName = settings.namespaceName + "." + settings.InstallerName;
            return TypeCache.GetTypesDerivedFrom(baseType).FirstOrDefault(t => string.Equals(t.FullName, fullName, StringComparison.Ordinal));
        }

        /// <summary>The settings asset VContainer loads, or null when there is none in the preloaded assets.</summary>
        public static Object PreloadedSettings()
        {
            Type settingsType = SettingsType;
            return settingsType == null ? null : PlayerSettings.GetPreloadedAssets().FirstOrDefault(a => a != null && settingsType.IsInstanceOfType(a));
        }

        public static Object RootScopeOf(Object vcontainerSettings)
        {
            return vcontainerSettings == null ? null : new SerializedObject(vcontainerSettings).FindProperty("RootLifetimeScope").objectReferenceValue;
        }

        /// <summary>Creates or repairs every asset. Returns one line per action for the tab.</summary>
        public static IReadOnlyList<string> Apply(CompositionSettings settings)
        {
            Type scopeType = ScopeType ?? throw new InvalidOperationException("Install VContainer and Pretzel Integrations first.");
            Type installerType = FindInstallerType(settings)
                                 ?? throw new InvalidOperationException($"{settings.namespaceName}.{settings.InstallerName} is not compiled yet. Generate the code and wait for compilation.");

            List<string> log = new();
            EnsureFolder(settings.assetFolder);

            Object installer = AssetDatabase.LoadAssetAtPath(InstallerAssetPath(settings), installerType);
            if (installer == null)
            {
                installer = ScriptableObject.CreateInstance(installerType);
                AssetDatabase.CreateAsset(installer, InstallerAssetPath(settings));
                log.Add("Created " + InstallerAssetPath(settings));
            }

            Component scope = EnsureScopePrefab(settings, scopeType, installer, log);
            EnsureVContainerSettings(settings, scope, log);
            AssetDatabase.SaveAssets();
            return log;
        }

        private static Component EnsureScopePrefab(CompositionSettings settings, Type scopeType, Object installer, List<string> log)
        {
            string path = ScopePrefabPath(settings);
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(settings.ScopePrefabName);
            try
            {
                Component scope = root.GetComponent(scopeType);
                if (scope == null)
                {
                    scope = root.AddComponent(scopeType);
                }

                SerializedObject serialized = new(scope);
                SerializedProperty installers = serialized.FindProperty("_installers");
                if (!Contains(installers, installer))
                {
                    installers.InsertArrayElementAtIndex(installers.arraySize);
                    installers.GetArrayElementAtIndex(installers.arraySize - 1).objectReferenceValue = installer;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    log.Add($"Added {settings.InstallerName} to {settings.ScopePrefabName}");
                }

                AddAudioManager(settings, root, log);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                if (!exists)
                {
                    log.Add("Created " + path);
                }
            }
            finally
            {
                if (exists)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
                else
                {
                    Object.DestroyImmediate(root);
                }
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent(scopeType);
        }

        private static void AddAudioManager(CompositionSettings settings, GameObject root, List<string> log)
        {
            Type audioType = Type.GetType(AudioManagerTypeName);
            bool wanted = settings.pretzelServices.Contains(PretzelServiceCatalog.Audio, StringComparer.OrdinalIgnoreCase);
            if (!wanted || audioType == null || root.GetComponentInChildren(audioType, true) != null)
            {
                return;
            }

            GameObject child = new("AudioManager");
            child.transform.SetParent(root.transform, false);
            child.AddComponent(audioType);
            log.Add("Added an AudioManager child to " + settings.ScopePrefabName);
        }

        private static void EnsureVContainerSettings(CompositionSettings settings, Component scope, List<string> log)
        {
            Type settingsType = SettingsType;
            Object asset = PreloadedSettings();
            if (asset == null)
            {
                string existing = AssetDatabase.FindAssets("t:" + settingsType.Name).Select(guid => AssetDatabase.GUIDToAssetPath(guid)).FirstOrDefault();
                asset = existing != null ? AssetDatabase.LoadAssetAtPath(existing, settingsType) : null;
            }

            if (asset == null)
            {
                string path = settings.assetFolder + "/VContainerSettings.asset";
                asset = ScriptableObject.CreateInstance(settingsType);
                AssetDatabase.CreateAsset(asset, path);
                log.Add("Created " + path);
            }

            SerializedObject serialized = new(asset);
            SerializedProperty root = serialized.FindProperty("RootLifetimeScope");
            if (root.objectReferenceValue == null)
            {
                root.objectReferenceValue = scope;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                log.Add("Set the VContainer root scope to " + settings.ScopePrefabName);
            }
            else if (root.objectReferenceValue != scope)
            {
                log.Add($"VContainerSettings already uses {root.objectReferenceValue.name} as the root scope; left unchanged.");
            }

            List<Object> preloaded = PlayerSettings.GetPreloadedAssets().Where(a => a != null).ToList();
            if (!preloaded.Contains(asset))
            {
                preloaded.RemoveAll(settingsType.IsInstanceOfType);
                preloaded.Add(asset);
                PlayerSettings.SetPreloadedAssets(preloaded.ToArray());
                log.Add("Added VContainerSettings to the preloaded assets");
            }
        }

        private static bool Contains(SerializedProperty array, Object value)
        {
            for (int i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == value)
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            int slash = folder.LastIndexOf('/');
            EnsureFolder(folder.Substring(0, slash));
            AssetDatabase.CreateFolder(folder.Substring(0, slash), folder.Substring(slash + 1));
        }
    }
}
