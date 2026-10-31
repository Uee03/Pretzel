using UnityEditor;
using UnityEngine;

namespace Pretzel.Multiplayer.Editor
{
    /// <summary>Sets the serialized fields of a <see cref="PretzelNetworkBootstrap"/> from Editor code (undoable, prefab-override aware).</summary>
    public static class NetworkBootstrapEditorUtility
    {
        public static void SetPlayerPrefab(PretzelNetworkBootstrap bootstrap, GameObject playerPrefab)
        {
            SerializedObject serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("_playerPrefab").objectReferenceValue = playerPrefab;
            serialized.ApplyModifiedProperties();
        }

        public static void SetSpawnPoints(PretzelNetworkBootstrap bootstrap, Transform[] points)
        {
            SerializedObject serialized = new SerializedObject(bootstrap);
            SerializedProperty array = serialized.FindProperty("_spawnPoints");
            array.arraySize = points.Length;
            for (int i = 0; i < points.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
            }

            serialized.ApplyModifiedProperties();
        }

        public static void AddPooledPrefab(PretzelNetworkBootstrap bootstrap, GameObject prefab, int prewarm, int maxIdle)
        {
            SerializedObject serialized = new SerializedObject(bootstrap);
            SerializedProperty array = serialized.FindProperty("_pooledPrefabs");
            int index = array.arraySize;
            array.InsertArrayElementAtIndex(index);
            SerializedProperty entry = array.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("_prefab").objectReferenceValue = prefab;
            entry.FindPropertyRelative("_prewarm").intValue = prewarm;
            entry.FindPropertyRelative("_maxIdle").intValue = maxIdle;
            serialized.ApplyModifiedProperties();
        }
    }
}
