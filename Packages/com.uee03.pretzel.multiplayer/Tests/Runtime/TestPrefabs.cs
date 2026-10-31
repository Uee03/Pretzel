#if UNITY_EDITOR
using System;
using Pretzel.Multiplayer;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace Pretzel.Multiplayer.PlayModeTests
{
    /// <summary>
    /// Network prefabs must be assets with a GlobalObjectIdHash, so the PlayMode tests create temporary prefab assets through the Editor.
    /// That is why these tests run in the Editor only.
    /// </summary>
    public static class TestPrefabs
    {
        public static GameObject Save(GameObject root, string assetPath)
        {
            string folder = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(folder).Replace('\\', '/'), System.IO.Path.GetFileName(folder));
            }

            GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset.GetComponent<NetworkObject>().PrefabIdHash == 0)
            {
                throw new InvalidOperationException("NGO did not assign a GlobalObjectIdHash to " + assetPath);
            }

            return asset;
        }

        public static void Configure(PretzelNetworkBootstrap bootstrap, GameObject playerPrefab, GameObject pooledPrefab, int prewarm, int maxIdle)
        {
            SerializedObject serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("_playerPrefab").objectReferenceValue = playerPrefab;
            SerializedProperty array = serialized.FindProperty("_pooledPrefabs");
            array.arraySize = 1;
            SerializedProperty entry = array.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("_prefab").objectReferenceValue = pooledPrefab;
            entry.FindPropertyRelative("_prewarm").intValue = prewarm;
            entry.FindPropertyRelative("_maxIdle").intValue = maxIdle;
            serialized.FindProperty("_offerRejoinOnStart").boolValue = false;
            serialized.ApplyModifiedProperties();
        }

        public static void Delete(string folder)
        {
            AssetDatabase.DeleteAsset(folder);
        }
    }
}
#endif
