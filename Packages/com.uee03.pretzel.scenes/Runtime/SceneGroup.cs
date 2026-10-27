using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pretzel.Scenes
{
    /// <summary>
    /// Scenes that make up one level, such as gameplay, environment, lighting and audio, loaded and unloaded together.
    /// Names must match scenes in the build profile's scene list.
    /// </summary>
    [CreateAssetMenu(menuName = "Pretzel/Scenes/Scene Group", fileName = "SceneGroup")]
    public sealed class SceneGroup : ScriptableObject
    {
        [Tooltip("Loaded in this order. With a single-mode load, the first one replaces the open scenes.")]
        [SerializeField] private List<string> _scenes = new();

        [Tooltip("Scene that becomes active, which receives new objects and decides lighting. Empty uses the first scene.")]
        [SerializeField] private string _activeScene;

        public IReadOnlyList<string> Scenes => _scenes;

        public string ActiveScene => string.IsNullOrEmpty(_activeScene) && _scenes.Count > 0 ? _scenes[0] : _activeScene;

        /// <summary>Creates a group from code, for tests and generated content.</summary>
        public static SceneGroup Create(string activeScene, params string[] scenes)
        {
            SceneGroup group = CreateInstance<SceneGroup>();
            group._scenes = new List<string>(scenes ?? Array.Empty<string>());
            group._activeScene = activeScene;
            return group;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            HashSet<string> inBuild = new(StringComparer.Ordinal);
            foreach (UnityEditor.EditorBuildSettingsScene scene in UnityEditor.EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    inBuild.Add(System.IO.Path.GetFileNameWithoutExtension(scene.path));
                }
            }

            foreach (string scene in _scenes)
            {
                if (!string.IsNullOrEmpty(scene) && !inBuild.Contains(scene))
                {
                    Debug.LogWarning($"{name}: '{scene}' is not an enabled scene in the build profile's scene list.", this);
                }
            }
        }
#endif
    }
}
