using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Pretzel.Cameras.Editor
{
    /// <summary>
    /// Builds a ready camera rig: a main camera with a CinemachineBrain and a CinemachineCamera set up for 2D, 2.5D or
    /// 3D, with the Pretzel shake already attached. Every change is registered for Undo.
    /// </summary>
    public static class CameraRigBuilder
    {
        public static CinemachineCamera Create(CameraRigKind kind, Transform follow = null)
        {
            Camera camera = EnsureMainCamera();
            if (camera.GetComponent<CinemachineBrain>() == null)
            {
                Undo.AddComponent<CinemachineBrain>(camera.gameObject);
            }

            Undo.RecordObject(camera, "Set up camera rig");
            camera.orthographic = kind == CameraRigKind.TwoD;

            GameObject rig = new(NameOf(kind));
            Undo.RegisterCreatedObjectUndo(rig, "Create camera rig");
            CinemachineCamera vcam = Undo.AddComponent<CinemachineCamera>(rig);
            vcam.Follow = follow;

            switch (kind)
            {
                case CameraRigKind.TwoD:
                    SetUpTwoD(rig, vcam);
                    break;
                case CameraRigKind.TwoPointFiveD:
                    SetUpTwoPointFiveD(rig, vcam);
                    break;
                default:
                    SetUpThreeD(rig, vcam);
                    break;
            }

            Undo.AddComponent<CameraShake>(rig);
            return vcam;
        }

        private static void SetUpTwoD(GameObject rig, CinemachineCamera vcam)
        {
            vcam.Lens.OrthographicSize = 5f;
            rig.transform.position = new Vector3(0f, 0f, -10f);

            CinemachinePositionComposer composer = Undo.AddComponent<CinemachinePositionComposer>(rig);
            composer.CameraDistance = 10f;
            composer.Damping = new Vector3(0.5f, 0.5f, 0f);

            PlayfieldFitter fitter = Undo.AddComponent<PlayfieldFitter>(rig);
            fitter.Configure(new Vector2(16f, 9f));
        }

        private static void SetUpTwoPointFiveD(GameObject rig, CinemachineCamera vcam)
        {
            vcam.Lens.FieldOfView = 50f;
            rig.transform.position = new Vector3(0f, 0f, -12f);

            CinemachinePositionComposer composer = Undo.AddComponent<CinemachinePositionComposer>(rig);
            composer.CameraDistance = 12f;
            composer.Damping = new Vector3(0.6f, 0.6f, 0.6f);
        }

        private static void SetUpThreeD(GameObject rig, CinemachineCamera vcam)
        {
            vcam.Lens.FieldOfView = 60f;
            rig.transform.position = new Vector3(0f, 3f, -8f);
            Undo.AddComponent<CinemachineOrbitalFollow>(rig);
            Undo.AddComponent<CinemachineRotationComposer>(rig);
        }

        private static Camera EnsureMainCamera()
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                return camera;
            }

            GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            Undo.RegisterCreatedObjectUndo(cameraObject, "Create camera rig");
            return cameraObject.GetComponent<Camera>();
        }

        private static string NameOf(CameraRigKind kind)
        {
            switch (kind)
            {
                case CameraRigKind.TwoD:
                    return "CM 2D Camera";
                case CameraRigKind.TwoPointFiveD:
                    return "CM 2.5D Camera";
                default:
                    return "CM 3D Camera";
            }
        }
    }
}
