using UnityEditor;
using UnityEngine;

namespace Pretzel.Parallax.Editor
{
    /// <summary>GameObject menu entry that creates a controller with two layers, ready to receive sprites.</summary>
    internal static class ParallaxMenu
    {
        [MenuItem("GameObject/Pretzel/Parallax/Parallax Background", false, 10)]
        private static void CreateBackground(MenuCommand command)
        {
            GameObject root = new("Parallax Background");
            GameObjectUtility.SetParentAndAlign(root, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(root, "Create Parallax Background");

            ParallaxController controller = Undo.AddComponent<ParallaxController>(root);
            Camera main = Camera.main;
            if (main != null)
            {
                controller.SetCamera(main.transform);
            }

            CreateLayer(root.transform, "Far Layer", new Vector2(0.8f, 0.8f), 10f);
            CreateLayer(root.transform, "Near Layer", new Vector2(0.4f, 0.4f), 5f);
            Selection.activeGameObject = root;
        }

        private static void CreateLayer(Transform parent, string name, Vector2 factor, float z)
        {
            GameObject layerObject = new(name, typeof(SpriteRenderer));
            Undo.RegisterCreatedObjectUndo(layerObject, "Create Parallax Layer");
            layerObject.transform.SetParent(parent, false);
            layerObject.transform.localPosition = new Vector3(0f, 0f, z);

            ParallaxLayer layer = Undo.AddComponent<ParallaxLayer>(layerObject);
            layer.Configure(factor, repeatX: true);
        }
    }
}
