using Pretzel.Audio;
using Pretzel.Integrations.VContainer;
using Pretzel.Scenes;
using Pretzel.Sfx;
using SpaceInvaders.Audio;
using SpaceInvaders.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpaceInvaders.Editor
{
    /// <summary>
    /// Menu entry points that build the whole demo from code: sounds, waves, panels, prefabs, the root scope's loading screen and both
    /// scenes. Every step is repeatable, and a rebuild keeps designers' edits to wave and cue assets.
    /// </summary>
    public static class DemoBuilder
    {
        private const string RootScopePath = "Assets/_Project/Settings/Composition/GameLifetimeScope.prefab";
        private const string LoadingLayoutPath = "Assets/_Project/UI/Layouts/Loading.uxml";

        [MenuItem("Space Invaders/Build Demo", priority = 1)]
        public static void BuildAll()
        {
            BuilderUtil.EnsureFolder("Assets/_Project/Scenes");
            ArtImporter.Import();
            SoundBankBuilder.Build();
            WaveAssetsBuilder.Build();
            AssetDatabase.SaveAssets();
            PanelAssetsBuilder.BuildAll();
            ActorPrefabsBuilder.Build();
            ConfigureRootScope();
            SceneBuilder.BuildTitle();
            SceneBuilder.BuildGame();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SceneBuilder.TitleScenePath, true),
                new EditorBuildSettingsScene(SceneBuilder.GameScenePath, true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("[SI] Demo built: Title and Game scenes are in the build settings.");
        }

        [MenuItem("Space Invaders/Rebuild Sound Library", priority = 20)]
        public static void RegenerateSounds()
        {
            SoundBankBuilder.Build();
            AssetDatabase.SaveAssets();
            Debug.Log("[SI] Sound library rebuilt from " + SoundBankBuilder.SfxFolder);
        }

        /// <summary>Puts the settings assets and the loading screen on the root scope prefab that the Pretzel VContainer tab created.</summary>
        private static void ConfigureRootScope()
        {
            SoundBank sounds = AssetDatabase.LoadAssetAtPath<SoundBank>(SoundBankBuilder.SoundBankPath);
            SfxLibrary sfx = AssetDatabase.LoadAssetAtPath<SfxLibrary>(SoundBankBuilder.SfxLibraryPath);
            WaveSet waves = AssetDatabase.LoadAssetAtPath<WaveSet>(WaveAssetsBuilder.WaveSetPath);
            PanelSettings loadingPanel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelAssetsBuilder.LoadingPath);
            GameObject root = PrefabUtility.LoadPrefabContents(RootScopePath);
            try
            {
                PretzelLifetimeScope scope = root.GetComponent<PretzelLifetimeScope>();
                BuilderUtil.SetObjects(scope, "_settings", new Object[] { sounds, sfx, waves });

                UIDocumentSceneTransition transition = root.GetComponentInChildren<UIDocumentSceneTransition>(true);
                if (transition == null)
                {
                    GameObject screen = new("LoadingScreen");
                    screen.transform.SetParent(root.transform, false);
                    screen.AddComponent<UIDocument>();
                    transition = screen.AddComponent<UIDocumentSceneTransition>();
                }

                UIDocument document = transition.GetComponent<UIDocument>();
                document.panelSettings = loadingPanel;
                document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LoadingLayoutPath);
                BuilderUtil.SetObject(transition, "_document", document);

                // Left on: a transition under the persistent root scope prefab is not moved out of it.
                BuilderUtil.SetBool(transition, "_persistAcrossLoads", true);
                PrefabUtility.SaveAsPrefabAsset(root, RootScopePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
