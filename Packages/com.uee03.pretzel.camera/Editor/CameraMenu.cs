using UnityEditor;
using UnityEngine;

namespace Pretzel.Cameras.Editor
{
    internal static class CameraMenu
    {
        [MenuItem("GameObject/Pretzel/Camera/2D Camera Rig", false, 20)]
        private static void CreateTwoD() => Create(CameraRigKind.TwoD);

        [MenuItem("GameObject/Pretzel/Camera/2.5D Camera Rig", false, 21)]
        private static void CreateTwoPointFiveD() => Create(CameraRigKind.TwoPointFiveD);

        [MenuItem("GameObject/Pretzel/Camera/3D Camera Rig", false, 22)]
        private static void CreateThreeD() => Create(CameraRigKind.ThreeD);

        private static void Create(CameraRigKind kind)
        {
            Transform follow = Selection.activeTransform;
            Selection.activeGameObject = CameraRigBuilder.Create(kind, follow).gameObject;
        }
    }
}
