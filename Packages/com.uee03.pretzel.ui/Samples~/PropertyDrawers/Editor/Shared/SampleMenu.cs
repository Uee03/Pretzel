using UnityEditor;
using UnityEngine;

namespace Pretzel.Samples.UI.PropertyDrawers.Editors
{
    /// <summary>Creates demo GameObjects, one per drawer level, so the drawers can be tried without wiring anything up.</summary>
    internal static class SampleMenu
    {
        private const string Root = "Pretzel/Samples/Property Drawers/Create/";

        [MenuItem(Root + "1 Basic Drawer Demo")]
        private static void CreateBasic()
        {
            Create<BasicDrawerDemo>("Basic Drawer Demo");
        }

        [MenuItem(Root + "2 Intermediate Drawer Demo")]
        private static void CreateIntermediate()
        {
            Create<IntermediateDrawerDemo>("Intermediate Drawer Demo");
        }

        [MenuItem(Root + "3 Advanced Drawer Demo")]
        private static void CreateAdvanced()
        {
            Create<AdvancedDrawerDemo>("Advanced Drawer Demo");
        }

        private static void Create<T>(string name) where T : Component
        {
            GameObject instance = new(name, typeof(T));
            Undo.RegisterCreatedObjectUndo(instance, "Create " + name);
            Selection.activeGameObject = instance;
        }
    }
}
