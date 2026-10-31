using System;
using Pretzel.UI.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.CustomInspectors.Editors
{
    /// <summary>
    /// A component editor whose buttons change the scene. Every change goes through Undo, instances stay linked to their
    /// prefab, prefab-instance overrides are recorded and prefab assets (which have no scene to change) disable the buttons.
    /// </summary>
    [CustomEditor(typeof(SpawnArea))]
    [CanEditMultipleObjects]
    public sealed class SpawnAreaEditor : UnityEditor.Editor
    {
        private Label _summary;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();
            root.AddToClassList("pretzel-spawn-area");
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            bool editable = !AnyPersistent();
            if (!editable)
            {
                EditorUI.Help("This is a prefab asset. Open it in Prefab Mode or select an instance in a scene to use the buttons.", HelpBoxMessageType.Info, root);
            }

            _summary = new Label();
            _summary.AddToClassList("pretzel-spawn-area__summary");
            root.Add(_summary);

            VisualElement buttons = EditorUI.ButtonRow(
                ("Spawn", SpawnAll),
                ("Scatter", ScatterAll),
                ("Clear", ClearAll));
            buttons.SetEnabled(editable);
            root.Add(buttons);

            root.TrackSerializedObjectValue(serializedObject, OnSerializedObjectChanged);
            root.RegisterCallback<AttachToPanelEvent>(OnAttach);
            root.RegisterCallback<DetachFromPanelEvent>(OnDetach);
            Refresh();
            return root;
        }

        // Undo and redo can add or remove the spawned children without touching the serialized fields.
        private void OnAttach(AttachToPanelEvent evt)
        {
            Undo.undoRedoPerformed += Refresh;
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            Undo.undoRedoPerformed -= Refresh;
        }

        private void OnSerializedObjectChanged(SerializedObject changed)
        {
            Refresh();
        }

        private bool AnyPersistent()
        {
            foreach (UnityEngine.Object item in targets)
            {
                if (EditorUtility.IsPersistent(item))
                {
                    return true;
                }
            }

            return false;
        }

        private void SpawnAll()
        {
            ForEachArea("Spawn", Spawn);
        }

        private void ScatterAll()
        {
            ForEachArea("Scatter", Scatter);
        }

        private void ClearAll()
        {
            ForEachArea("Clear", Clear);
        }

        // One Undo step for the whole button press, however many targets are selected.
        private void ForEachArea(string undoName, Action<SpawnArea> action)
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            foreach (UnityEngine.Object item in targets)
            {
                action((SpawnArea)item);
            }

            Undo.CollapseUndoOperations(group);
            Refresh();
        }

        private static void Spawn(SpawnArea area)
        {
            if (!area.prefab)
            {
                Debug.LogWarning($"{area.name} has no prefab to spawn.", area);
                return;
            }

            Transform container = FindContainer(area) ?? CreateContainer(area);
            System.Random random = new(area.seed);
            for (int i = 0; i < area.count; i++)
            {
                GameObject instance = InstantiateLinked(area.prefab, container);
                Undo.RegisterCreatedObjectUndo(instance, "Spawn");
                instance.transform.localPosition = area.SamplePoint(random);
            }
        }

        private static void Scatter(SpawnArea area)
        {
            Transform container = FindContainer(area);
            if (container == null)
            {
                return;
            }

            Undo.RecordObject(area, "Scatter");
            area.seed++;
            PrefabUtility.RecordPrefabInstancePropertyModifications(area);

            System.Random random = new(area.seed);
            for (int i = 0; i < container.childCount; i++)
            {
                Transform child = container.GetChild(i);
                Undo.RecordObject(child, "Scatter");
                child.localPosition = area.SamplePoint(random);
                PrefabUtility.RecordPrefabInstancePropertyModifications(child);
            }
        }

        private static void Clear(SpawnArea area)
        {
            Transform container = FindContainer(area);
            if (container == null)
            {
                return;
            }

            // Objects that come from the prefab asset cannot be destroyed on an instance; added ones can.
            GameObject go = container.gameObject;
            if (PrefabUtility.IsPartOfPrefabInstance(go) && !PrefabUtility.IsAddedGameObjectOverride(go))
            {
                Debug.LogWarning($"'{ContainerPath(area)}' belongs to the prefab. Edit the prefab asset to remove it.", area);
                return;
            }

            Undo.DestroyObjectImmediate(go);
        }

        private static Transform FindContainer(SpawnArea area)
        {
            return area.transform.Find(SpawnArea.ContainerName);
        }

        private static Transform CreateContainer(SpawnArea area)
        {
            GameObject container = new(SpawnArea.ContainerName);
            Undo.RegisterCreatedObjectUndo(container, "Spawn");
            Undo.SetTransformParent(container.transform, area.transform, "Spawn");
            container.transform.localPosition = Vector3.zero;
            container.transform.localRotation = Quaternion.identity;
            container.transform.localScale = Vector3.one;
            return container.transform;
        }

        // Keeps the prefab link when the source is a prefab asset, so overrides and updates keep working.
        private static GameObject InstantiateLinked(GameObject prefab, Transform parent)
        {
            if (PrefabUtility.IsPartOfPrefabAsset(prefab))
            {
                return (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            }

            return UnityEngine.Object.Instantiate(prefab, parent);
        }

        private static string ContainerPath(SpawnArea area)
        {
            return area.name + "/" + SpawnArea.ContainerName;
        }

        private void Refresh()
        {
            int spawned = 0;
            foreach (UnityEngine.Object item in targets)
            {
                if (!item)
                {
                    continue;
                }

                Transform container = FindContainer((SpawnArea)item);
                spawned += container != null ? container.childCount : 0;
            }

            _summary.text = "Spawned objects: " + spawned;
        }
    }
}
