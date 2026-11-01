using System.IO;
using Pretzel.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Pretzel.Sfx.Samples.Editor
{
    /// <summary>
    /// Turns the pack's WAV files into audio cues and an SfxLibrary, and builds the sound board scene. The sample ships
    /// with these built; run it again after changing the sounds or the table below.
    /// </summary>
    public static class SfxPackBuilder
    {
        // Id, channel, cooldown seconds, most voices at once, and the pitch range that keeps repeats from sounding identical.
        private static readonly (string Id, AudioChannel Channel, float Cooldown, int MaxVoices, float PitchMin, float PitchMax)[] _sounds =
        {
            ("ui_click", AudioChannel.UI, 0.03f, 0, 0.97f, 1.03f),
            ("ui_confirm", AudioChannel.UI, 0.1f, 0, 1f, 1f),
            ("ui_back", AudioChannel.UI, 0.1f, 0, 1f, 1f),
            ("ui_error", AudioChannel.UI, 0.15f, 0, 1f, 1f),
            ("pickup_coin", AudioChannel.Sfx, 0.05f, 4, 0.95f, 1.08f),
            ("power_up", AudioChannel.Sfx, 0f, 1, 1f, 1f),
            ("jump", AudioChannel.Sfx, 0.1f, 0, 0.96f, 1.06f),
            ("laser", AudioChannel.Sfx, 0.07f, 4, 0.92f, 1.08f),
            ("hit_light", AudioChannel.Sfx, 0.08f, 3, 0.92f, 1.08f),
            ("hit_heavy", AudioChannel.Sfx, 0f, 2, 0.95f, 1.03f),
            ("explosion", AudioChannel.Sfx, 0f, 2, 0.9f, 1.05f),
            ("stinger_win", AudioChannel.Sfx, 0f, 1, 1f, 1f),
            ("stinger_lose", AudioChannel.Sfx, 0f, 1, 1f, 1f)
        };

        [MenuItem("Pretzel/Samples/SFX/Build Library And Scene")]
        public static void Build()
        {
            string root = SampleRoot();
            Directory.CreateDirectory(root + "/Cues");
            Directory.CreateDirectory(root + "/Scenes");
            AssetDatabase.Refresh();

            System.Collections.Generic.List<SfxEntry> entries = new();
            foreach ((string id, AudioChannel channel, float cooldown, int maxVoices, float pitchMin, float pitchMax) in _sounds)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(root + "/Audio/" + id + ".wav");
                if (clip == null)
                {
                    throw new FileNotFoundException("Missing sound: " + id + ".wav");
                }

                AudioCue cue = CreateCue(root + "/Cues/" + id + ".asset", clip, channel, pitchMin, pitchMax);
                entries.Add(new SfxEntry(id, cue, cooldown, maxVoices));
            }

            string libraryPath = root + "/SfxLibrary.asset";
            SfxLibrary library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(libraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<SfxLibrary>();
                AssetDatabase.CreateAsset(library, libraryPath);
            }

            library.SetEntries(entries);
            EditorUtility.SetDirty(library);
            CreatePanelSettings(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            BuildScene(root, root + "/Scenes/SfxBoard.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("[SFX] Built the library and the sound board scene. Open SfxBoard and press Play.");
        }

        private static string SampleRoot()
        {
            foreach (string guid in AssetDatabase.FindAssets("SfxPackBuilder t:MonoScript"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("/SfxPackBuilder.cs", System.StringComparison.Ordinal))
                {
                    return Path.GetDirectoryName(Path.GetDirectoryName(path)).Replace('\\', '/');
                }
            }

            throw new FileNotFoundException("Cannot locate the SFX pack folder.");
        }

        private static AudioCue CreateCue(string path, AudioClip clip, AudioChannel channel, float pitchMin, float pitchMax)
        {
            AudioCue cue = AssetDatabase.LoadAssetAtPath<AudioCue>(path);
            if (cue == null)
            {
                cue = ScriptableObject.CreateInstance<AudioCue>();
                AssetDatabase.CreateAsset(cue, path);
            }

            SerializedObject serialized = new(cue);
            SerializedProperty clips = serialized.FindProperty("_clips");
            clips.arraySize = 1;
            clips.GetArrayElementAtIndex(0).objectReferenceValue = clip;
            serialized.FindProperty("_channel").intValue = (int)channel;
            serialized.FindProperty("_pitch").vector2Value = new Vector2(pitchMin, pitchMax);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return cue;
        }

        private static void CreatePanelSettings(string root)
        {
            string path = root + "/Styles/SfxBoardPanel.asset";
            PanelSettings panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, path);
            }

            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(root + "/Styles/SfxBoardTheme.tss");
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1280, 720);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            EditorUtility.SetDirty(panel);
        }

        private static void BuildScene(string root, string path)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.11f, 0.15f);

            GameObject audioObject = new("Audio Service");
            AudioService audio = audioObject.AddComponent<AudioService>();

            GameObject board = new("Sound Board", typeof(UIDocument));
            board.GetComponent<UIDocument>().panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(root + "/Styles/SfxBoardPanel.asset");
            SfxBoard component = board.AddComponent<SfxBoard>();
            SerializedObject serialized = new(component);
            serialized.FindProperty("_library").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SfxLibrary>(root + "/SfxLibrary.asset");
            serialized.FindProperty("_audio").objectReferenceValue = audio;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
