#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Pretzel.Scenes.Tests
{
    /// <summary>Creates the test scenes and adds them to the build scene list before the PlayMode run, and removes them after.</summary>
    public sealed class SceneLoaderTestScenes : IPrebuildSetup, IPostBuildCleanup
    {
        private const string Folder = "Assets/PretzelSceneLoaderTests";

        public void Setup()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets", "PretzelSceneLoaderTests");
            }

            List<EditorBuildSettingsScene> buildScenes = new(EditorBuildSettings.scenes);
            foreach (string name in TestSceneNames.All)
            {
                string path = $"{Folder}/{name}.unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                {
                    // Additive, so the scene the test runner opened stays open.
                    Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                    SceneManager.MoveGameObjectToScene(new GameObject(TestSceneNames.Marker(name)), scene);
                    EditorSceneManager.SaveScene(scene, path);
                    EditorSceneManager.CloseScene(scene, true);
                }

                if (!buildScenes.Exists(entry => string.Equals(entry.path, path, StringComparison.Ordinal)))
                {
                    buildScenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
        }

        public void Cleanup()
        {
            List<EditorBuildSettingsScene> buildScenes = new(EditorBuildSettings.scenes);
            buildScenes.RemoveAll(entry => entry.path.StartsWith(Folder + "/", StringComparison.Ordinal));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.DeleteAsset(Folder);
        }
    }
}
#endif
