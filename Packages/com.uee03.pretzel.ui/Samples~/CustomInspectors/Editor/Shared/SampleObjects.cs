using UnityEditor;
using UnityEngine;

namespace Pretzel.Samples.UI.CustomInspectors.Editors
{
    /// <summary>Creates demo GameObjects from the sample menus with Undo and selection.</summary>
    internal static class SampleObjects
    {
        public const string MenuRoot = "Pretzel/Samples/Custom Inspectors/";

        public static T CreateComponent<T>(string name) where T : Component
        {
            GameObject instance = new(name, typeof(T));
            Undo.RegisterCreatedObjectUndo(instance, "Create " + name);
            Selection.activeGameObject = instance;
            return instance.GetComponent<T>();
        }
    }
}
