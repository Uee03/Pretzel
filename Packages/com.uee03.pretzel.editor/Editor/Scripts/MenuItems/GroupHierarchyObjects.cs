using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Pretzel.Editor.MenuItems
{
    /// <summary>Parents the selected scene objects under one new empty object, with undo.</summary>
    public static class GroupHierarchyObjects
    {
        private const string MenuPath = "GameObject/Pretzel/Group Selected Objects";

        private static Transform[] SelectedRoots()
        {
            return Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.ExcludePrefab | SelectionMode.Editable);
        }

        [MenuItem(MenuPath, true)]
        private static bool Validate() => SelectedRoots().Length > 0;

        [MenuItem(MenuPath, false, 1)]
        private static void Group(MenuCommand command)
        {
            // The GameObject menu runs once per selected object. Only the active one does the work.
            if (command.context != null && command.context != Selection.activeGameObject)
            {
                return;
            }

            Transform[] roots = SelectedRoots();
            if (roots.Length == 0)
            {
                return;
            }

            Transform first = roots.OrderBy(t => t.GetSiblingIndex()).First();
            Transform parentOfFirst = first.parent;
            int siblingIndex = first.GetSiblingIndex();

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();

            GameObject groupObject = new("Group");
            Undo.RegisterCreatedObjectUndo(groupObject, "Group Selected Objects");
            Transform groupTransform = groupObject.transform;

            groupTransform.position = roots.Aggregate(Vector3.zero, (sum, t) => sum + t.position) / roots.Length;
            if (parentOfFirst != null)
            {
                Undo.SetTransformParent(groupTransform, parentOfFirst, "Group Selected Objects");
                groupTransform.SetSiblingIndex(siblingIndex);
            }

            foreach (Transform root in roots)
            {
                Undo.SetTransformParent(root, groupTransform, "Group Selected Objects");
            }

            Selection.activeGameObject = groupObject;
            Undo.CollapseUndoOperations(group);

            // Rename mode needs the Hierarchy to have processed the new selection first.
            EditorApplication.delayCall += () => EditorApplication.ExecuteMenuItem("Edit/Rename");
        }
    }
}
