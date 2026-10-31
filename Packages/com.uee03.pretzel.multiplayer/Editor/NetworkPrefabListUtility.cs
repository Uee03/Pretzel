using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace Pretzel.Multiplayer.Editor
{
    /// <summary>Makes sure a NetworkManager has a network prefab list asset and registers prefabs in it.</summary>
    public static class NetworkPrefabListUtility
    {
        public const string DefaultFolder = "Assets/PretzelMultiplayer";

        /// <summary>Returns the NetworkManager's first prefab list, creating <c>NetworkPrefabs.asset</c> in <paramref name="folder"/> when it has none.</summary>
        public static NetworkPrefabsList EnsureList(NetworkManager networkManager, string folder = DefaultFolder)
        {
            NetworkPrefabs prefabs = networkManager.NetworkConfig.Prefabs;
            for (int i = 0; i < prefabs.NetworkPrefabsLists.Count; i++)
            {
                if (prefabs.NetworkPrefabsLists[i] != null)
                {
                    return prefabs.NetworkPrefabsLists[i];
                }
            }

            AssetFolderUtility.EnsureFolder(folder);
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/NetworkPrefabs.asset");
            NetworkPrefabsList list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            AssetDatabase.CreateAsset(list, path);
            Undo.RecordObject(networkManager, "Add network prefab list");
            prefabs.NetworkPrefabsLists.Add(list);
            EditorUtility.SetDirty(networkManager);
            return list;
        }

        /// <summary>
        /// Gives the NetworkManager one dedicated prefab list asset (created when missing) instead of the shared default list, so a sample or feature keeps its prefabs together.
        /// NGO itself creates <c>DefaultNetworkPrefabs.asset</c> at the Assets root when a NetworkManager is added; it is left alone.
        /// </summary>
        public static NetworkPrefabsList UseDedicatedList(NetworkManager networkManager, string folder, string fileName)
        {
            string path = folder + "/" + fileName;
            NetworkPrefabsList list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(path);
            if (list == null)
            {
                AssetFolderUtility.EnsureFolder(folder);
                list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
                AssetDatabase.CreateAsset(list, path);
            }

            Undo.RecordObject(networkManager, "Use dedicated network prefab list");
            networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
            networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(list);
            EditorUtility.SetDirty(networkManager);
            return list;
        }

        /// <summary>Adds the prefab to the NetworkManager's list unless it is already registered. Returns true when it was added.</summary>
        public static bool Register(NetworkManager networkManager, GameObject prefab, string folder = DefaultFolder)
        {
            if (networkManager.NetworkConfig.Prefabs.Contains(prefab))
            {
                return false;
            }

            NetworkPrefabsList list = EnsureList(networkManager, folder);
            list.Add(new NetworkPrefab { Prefab = prefab });
            EditorUtility.SetDirty(list);
            AssetDatabase.SaveAssetIfDirty(list);
            return true;
        }
    }
}
