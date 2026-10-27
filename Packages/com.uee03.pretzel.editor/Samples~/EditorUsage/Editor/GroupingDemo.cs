using UnityEditor;
using UnityEngine;

namespace Pretzel.Samples.EditorUtilities
{
    /// <summary>
    /// Creates a few scattered objects to try GameObject > Pretzel > Group Selected Objects on.
    /// Everything is recorded with Undo, so Edit > Undo removes the group and then the objects.
    /// </summary>
    public static class GroupingDemo
    {
        private const string MenuRoot = "Tools/Pretzel Samples/Editor/";
        private const string GroupMenuPath = "GameObject/Pretzel/Group Selected Objects";
        private const string UndoName = "Create Grouping Demo";

        private static readonly PrimitiveType[] _shapes = { PrimitiveType.Cube, PrimitiveType.Sphere, PrimitiveType.Capsule, PrimitiveType.Cylinder };

        [MenuItem(MenuRoot + "Create Objects To Group", true)]
        [MenuItem(MenuRoot + "Group Selection", true)]
        private static bool ValidateEditMode() => !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem(MenuRoot + "Create Objects To Group", false, 0)]
        private static void CreateObjectsToGroup()
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            GameObject[] created = new GameObject[_shapes.Length];
            for (int i = 0; i < _shapes.Length; i++)
            {
                GameObject shape = GameObject.CreatePrimitive(_shapes[i]);
                shape.name = "Demo " + _shapes[i];
                shape.transform.position = new Vector3(i * 2f - 3f, 0.5f, i % 2 * 2f);
                Undo.RegisterCreatedObjectUndo(shape, UndoName);
                created[i] = shape;
            }

            Undo.CollapseUndoOperations(undoGroup);
            Selection.objects = created;
            Debug.Log("[Pretzel Samples] Four demo objects are selected. Group them with GameObject > Pretzel > Group Selected Objects, "
                      + "the Hierarchy context menu, or Tools > Pretzel Samples > Editor > Group Selection.");
        }

        [MenuItem(MenuRoot + "Group Selection", false, 1)]
        private static void GroupSelection()
        {
            // Runs the Pretzel menu item exactly as the GameObject menu does, so the sample stays in sync with the package.
            if (!EditorApplication.ExecuteMenuItem(GroupMenuPath))
            {
                Debug.LogWarning("[Pretzel Samples] Select at least one editable scene object first.");
            }
        }
    }
}
