using NUnit.Framework;
using Pretzel.Cameras.Editor;
using Unity.Cinemachine;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Pretzel.Cameras.Tests
{
    public sealed class CameraRigBuilderTests
    {
        private GameObject _target;
        private CinemachineCamera _rig;

        [SetUp]
        public void SetUp()
        {
            // A clean scene, so the test never touches a camera that belongs to whatever scene is open.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _target = new GameObject("Target");
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void TwoD_IsOrthographicWithFollowShakeAndFit()
        {
            _rig = CameraRigBuilder.Create(CameraRigKind.TwoD, _target.transform);

            Assert.IsTrue(Camera.main.orthographic);
            Assert.IsNotNull(Camera.main.GetComponent<CinemachineBrain>());
            Assert.AreEqual(_target.transform, _rig.Follow);
            Assert.IsNotNull(_rig.GetComponent<CinemachinePositionComposer>());
            Assert.IsNotNull(_rig.GetComponent<CameraShake>());
            Assert.IsNotNull(_rig.GetComponent<PlayfieldFitter>());
        }

        [Test]
        public void TwoPointFiveD_IsPerspectiveAtAFixedDistance()
        {
            _rig = CameraRigBuilder.Create(CameraRigKind.TwoPointFiveD, _target.transform);

            Assert.IsFalse(Camera.main.orthographic);
            Assert.AreEqual(12f, _rig.GetComponent<CinemachinePositionComposer>().CameraDistance);
            Assert.IsNotNull(_rig.GetComponent<CameraShake>());
            Assert.IsNull(_rig.GetComponent<PlayfieldFitter>());
        }

        [Test]
        public void ThreeD_OrbitsTheTarget()
        {
            _rig = CameraRigBuilder.Create(CameraRigKind.ThreeD, _target.transform);

            Assert.IsFalse(Camera.main.orthographic);
            Assert.IsNotNull(_rig.GetComponent<CinemachineOrbitalFollow>());
            Assert.IsNotNull(_rig.GetComponent<CinemachineRotationComposer>());
        }

        [Test]
        public void ExistingMainCamera_IsReusedAndGetsOneBrain()
        {
            GameObject existing = new("Main Camera", typeof(Camera)) { tag = "MainCamera" };

            _rig = CameraRigBuilder.Create(CameraRigKind.TwoD);
            CinemachineCamera second = CameraRigBuilder.Create(CameraRigKind.ThreeD);

            Assert.AreEqual(existing, Camera.main.gameObject);
            Assert.IsNotNull(second);
            Assert.AreEqual(1, existing.GetComponents<CinemachineBrain>().Length);
        }

        [Test]
        public void WithoutATarget_TheRigStillBuilds()
        {
            _rig = CameraRigBuilder.Create(CameraRigKind.TwoD);

            Assert.IsNull(_rig.Follow);
        }
    }
}
