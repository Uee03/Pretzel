using NUnit.Framework;
using Pretzel.Multiplayer.Editor;
using Pretzel.Multiplayer.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Multiplayer.Tests
{
    public sealed class MultiplayerSceneBuilderTests
    {
        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(NetworkPrefabListUtility.DefaultFolder);
        }

        [Test]
        public void CreateNetworkManager_AddsManagerTransportBootstrapAndPrefabList()
        {
            GameObject go = MultiplayerSceneBuilder.CreateNetworkManager();

            NetworkManager manager = go.GetComponent<NetworkManager>();
            Assert.IsNotNull(manager);
            Assert.IsNotNull(go.GetComponent<UnityTransport>());
            Assert.IsNotNull(go.GetComponent<PretzelNetworkBootstrap>());
            Assert.AreSame(go.GetComponent<UnityTransport>(), manager.NetworkConfig.NetworkTransport);
            Assert.IsTrue(manager.NetworkConfig.ConnectionApproval);
            Assert.GreaterOrEqual(manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Count, 1);
        }

        [Test]
        public void CreateNetworkManager_WhenOneExists_CompletesItInsteadOfAddingASecond()
        {
            GameObject existing = new GameObject("Existing", typeof(NetworkManager), typeof(UnityTransport));

            GameObject result = MultiplayerSceneBuilder.CreateNetworkManager();

            Assert.AreSame(existing, result);
            int managers = 0;
            foreach (GameObject root in existing.scene.GetRootGameObjects())
            {
                managers += root.GetComponentsInChildren<NetworkManager>(true).Length;
            }

            Assert.AreEqual(1, managers);
            Assert.IsNotNull(existing.GetComponent<PretzelNetworkBootstrap>());
        }

        [Test]
        public void CreateMenu_WiresUIDocumentWithPackagedLayoutAndPanelSettings()
        {
            GameObject go = MultiplayerSceneBuilder.CreateMenu();

            UIDocument document = go.GetComponent<UIDocument>();
            Assert.IsNotNull(go.GetComponent<MultiplayerMenu>());
            Assert.IsNotNull(document.visualTreeAsset);
            Assert.AreEqual("MultiplayerMenu", document.visualTreeAsset.name);
            Assert.IsNotNull(document.panelSettings);
            Assert.IsNotNull(document.panelSettings.themeStyleSheet, "The packaged theme is wired up");
            string panelPath = AssetDatabase.GetAssetPath(document.panelSettings);
            Assert.IsTrue(panelPath.StartsWith("Assets/", System.StringComparison.Ordinal), panelPath);
        }

        [Test]
        public void CreateMenu_ReusesAnExistingPanelSettingsAsset()
        {
            GameObject first = MultiplayerSceneBuilder.CreateMenu();
            GameObject second = MultiplayerSceneBuilder.CreateMenu();

            Assert.AreSame(first.GetComponent<UIDocument>().panelSettings, second.GetComponent<UIDocument>().panelSettings);
        }

        [Test]
        public void CreateMenu_IsUndoable()
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            GameObject go = MultiplayerSceneBuilder.CreateMenu();
            Assert.IsNotNull(go);

            Undo.RevertAllDownToGroup(group);

            Assert.IsNull(Object.FindAnyObjectByType<MultiplayerMenu>());
        }

        [Test]
        public void BootstrapEditorUtility_SetsThePlayerPrefabAndPooledEntries()
        {
            GameObject host = new GameObject("Bootstrap", typeof(NetworkManager), typeof(UnityTransport), typeof(PretzelNetworkBootstrap));
            PretzelNetworkBootstrap bootstrap = host.GetComponent<PretzelNetworkBootstrap>();
            GameObject prefab = new GameObject("Prefab");

            NetworkBootstrapEditorUtility.SetPlayerPrefab(bootstrap, prefab);
            NetworkBootstrapEditorUtility.AddPooledPrefab(bootstrap, prefab, 5, 9);

            SerializedObject serialized = new SerializedObject(bootstrap);
            Assert.AreSame(prefab, serialized.FindProperty("_playerPrefab").objectReferenceValue);
            SerializedProperty entries = serialized.FindProperty("_pooledPrefabs");
            Assert.AreEqual(1, entries.arraySize);
            Assert.AreEqual(5, entries.GetArrayElementAtIndex(0).FindPropertyRelative("_prewarm").intValue);
            Assert.AreEqual(9, entries.GetArrayElementAtIndex(0).FindPropertyRelative("_maxIdle").intValue);
            Object.DestroyImmediate(prefab);
        }
    }
}
