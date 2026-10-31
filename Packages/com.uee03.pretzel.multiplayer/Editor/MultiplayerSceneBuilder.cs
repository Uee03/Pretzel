using Pretzel.Multiplayer.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Multiplayer.Editor
{
    /// <summary>
    /// Editor-side factories behind the <c>GameObject > Pretzel > Multiplayer</c> menu. Public so sample and project tools can reuse them.
    /// PanelSettings assets are created under the project's Assets folder because package folders are read-only.
    /// </summary>
    public static class MultiplayerSceneBuilder
    {
        public const string PackageRoot = "Packages/com.uee03.pretzel.multiplayer";
        public const string LayoutPath = PackageRoot + "/Runtime/UI/Layouts/MultiplayerMenu.uxml";
        public const string ThemePath = PackageRoot + "/Runtime/UI/Styles/Themes/PretzelMultiplayerTheme.tss";
        public const string PanelSettingsFileName = "MultiplayerPanelSettings.asset";

        /// <summary>
        /// Creates a NetworkManager with UnityTransport and a <see cref="PretzelNetworkBootstrap"/> on one GameObject.
        /// Returns the existing one (completing it if needed) when the scene already has a NetworkManager.
        /// </summary>
        public static GameObject CreateNetworkManager(GameObject parent = null, string assetFolder = NetworkPrefabListUtility.DefaultFolder)
        {
            NetworkManager existing = Object.FindAnyObjectByType<NetworkManager>();
            if (existing != null)
            {
                if (existing.GetComponent<PretzelNetworkBootstrap>() == null)
                {
                    Undo.AddComponent<PretzelNetworkBootstrap>(existing.gameObject);
                }

                ConfigureNetworkManager(existing, assetFolder);
                Debug.Log("[Pretzel.Multiplayer] The scene already has a NetworkManager. It was completed with the Pretzel defaults instead of creating a second one.", existing);
                return existing.gameObject;
            }

            GameObject go = ObjectFactory.CreateGameObject("Network Manager", typeof(NetworkManager), typeof(UnityTransport), typeof(PretzelNetworkBootstrap));
            Undo.RegisterCreatedObjectUndo(go, "Create Network Manager");
            if (parent != null)
            {
                GameObjectUtility.SetParentAndAlign(go, parent);
            }

            ConfigureNetworkManager(go.GetComponent<NetworkManager>(), assetFolder);
            Selection.activeGameObject = go;
            return go;
        }

        /// <summary>Points the NetworkManager at its UnityTransport, turns on connection approval and makes sure it has a prefab list.</summary>
        public static void ConfigureNetworkManager(NetworkManager networkManager, string assetFolder = NetworkPrefabListUtility.DefaultFolder)
        {
            Undo.RecordObject(networkManager, "Configure Network Manager");
            UnityTransport transport = networkManager.GetComponent<UnityTransport>();
            if (transport == null)
            {
                transport = Undo.AddComponent<UnityTransport>(networkManager.gameObject);
            }

            networkManager.NetworkConfig.NetworkTransport = transport;
            networkManager.NetworkConfig.ConnectionApproval = true;
            NetworkPrefabListUtility.EnsureList(networkManager, assetFolder);
            EditorUtility.SetDirty(networkManager);
        }

        /// <summary>Creates the menu GameObject: UIDocument with the packaged UXML and a PanelSettings (with the packaged theme) plus <see cref="MultiplayerMenu"/>.</summary>
        public static GameObject CreateMenu(GameObject parent = null, string assetFolder = NetworkPrefabListUtility.DefaultFolder)
        {
            VisualTreeAsset layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LayoutPath);
            if (layout == null)
            {
                throw new System.InvalidOperationException("Could not load " + LayoutPath + ". Is the package imported?");
            }

            GameObject go = ObjectFactory.CreateGameObject("Multiplayer Menu", typeof(UIDocument), typeof(MultiplayerMenu));
            Undo.RegisterCreatedObjectUndo(go, "Create Multiplayer Menu");
            if (parent != null)
            {
                GameObjectUtility.SetParentAndAlign(go, parent);
            }

            UIDocument document = go.GetComponent<UIDocument>();
            document.panelSettings = GetOrCreatePanelSettings(assetFolder);
            document.visualTreeAsset = layout;
            EditorUtility.SetDirty(document);
            Selection.activeGameObject = go;
            if (Object.FindAnyObjectByType<PretzelNetworkBootstrap>() == null)
            {
                Debug.Log("[Pretzel.Multiplayer] The menu needs a PretzelNetworkBootstrap. Use GameObject > Pretzel > Multiplayer > Network Manager (Pretzel defaults).", go);
            }

            return go;
        }

        /// <summary>Returns <c>Assets/PretzelMultiplayer/MultiplayerPanelSettings.asset</c>, creating it with the packaged theme and a 1280x720 reference resolution when missing.</summary>
        public static PanelSettings GetOrCreatePanelSettings(string folder = NetworkPrefabListUtility.DefaultFolder)
        {
            string path = folder + "/" + PanelSettingsFileName;
            PanelSettings existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (existing != null)
            {
                return existing;
            }

            AssetFolderUtility.EnsureFolder(folder);
            PanelSettings settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1280, 720);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
            return settings;
        }
    }
}
