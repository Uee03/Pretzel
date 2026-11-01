using System.Collections;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.TestTools;

namespace Pretzel.Cameras.PlayTests
{
    /// <summary>Runs the extensions inside a live Cinemachine pipeline: a Brain, a CinemachineCamera and real frames.</summary>
    public sealed class CameraPipelineTests
    {
        private static Vector3 RigPosition => new(0f, 0f, -10f);

        private GameObject _cameraObject;
        private GameObject _rig;
        private Camera _camera;

        [SetUp]
        public void SetUp()
        {
            _cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(CinemachineBrain)) { tag = "MainCamera" };
            _camera = _cameraObject.GetComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 5f;

            _rig = new GameObject("Rig", typeof(CinemachineCamera));
            _rig.transform.position = RigPosition;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_rig);
            Object.Destroy(_cameraObject);
        }

        [UnityTest]
        public IEnumerator Shake_MovesTheCameraThenReturnsItExactlyToRest()
        {
            CameraShake shake = _rig.AddComponent<CameraShake>();
            yield return null;
            yield return null;
            Vector3 rest = _camera.transform.position;

            shake.AddTrauma(1f);
            float deepest = 0f;
            for (int frame = 0; frame < 20; frame++)
            {
                yield return null;
                deepest = Mathf.Max(deepest, (_camera.transform.position - rest).magnitude);
            }

            Assert.Greater(deepest, 0.01f, "The camera never moved.");

            float waited = 0f;
            while (shake.Trauma > 0f && waited < 3f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return null;
            yield return null;
            Assert.AreEqual(0f, shake.Trauma);
            Assert.Less((_camera.transform.position - rest).magnitude, 0.0001f);
            Assert.Less(Quaternion.Angle(_camera.transform.rotation, Quaternion.identity), 0.001f);
        }

        [UnityTest]
        public IEnumerator Shake_DoesNotMoveACameraThatIsStill()
        {
            _rig.AddComponent<CameraShake>();
            yield return null;
            Vector3 rest = _camera.transform.position;

            for (int frame = 0; frame < 10; frame++)
            {
                yield return null;
                Assert.AreEqual(rest, _camera.transform.position);
            }
        }

        [UnityTest]
        public IEnumerator Shake_KeepsRunningWhenTimeIsFrozen()
        {
            CameraShake shake = _rig.AddComponent<CameraShake>();
            yield return null;
            shake.AddTrauma(1f);
            Time.timeScale = 0f;
            try
            {
                float waited = 0f;
                while (shake.Trauma > 0f && waited < 3f)
                {
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }

                Assert.AreEqual(0f, shake.Trauma, "The shake froze with the game.");
            }
            finally
            {
                Time.timeScale = 1f;
            }
        }

        [UnityTest]
        public IEnumerator Fitter_SetsTheOrthographicSizeForTheAspect()
        {
            PlayfieldFitter fitter = _rig.AddComponent<PlayfieldFitter>();
            fitter.Configure(new Vector2(16f, 9f));

            _camera.aspect = 1f;
            yield return null;
            yield return null;
            Assert.AreEqual(8f, _camera.orthographicSize, 0.001f, "Square screen: the width decides.");

            _camera.aspect = 16f / 9f;
            yield return null;
            yield return null;
            Assert.AreEqual(4.5f, _camera.orthographicSize, 0.001f, "16:9 screen: the height decides.");

            _camera.aspect = 32f / 9f;
            yield return null;
            yield return null;
            Assert.AreEqual(4.5f, _camera.orthographicSize, 0.001f, "Ultrawide screen: the height still decides.");
        }

        [UnityTest]
        public IEnumerator Fitter_PaddingGrowsTheView()
        {
            PlayfieldFitter fitter = _rig.AddComponent<PlayfieldFitter>();
            fitter.Configure(new Vector2(16f, 9f), padding: 1f);
            _camera.aspect = 16f / 9f;

            yield return null;
            yield return null;

            Assert.AreEqual(5.5f, _camera.orthographicSize, 0.001f);
        }

        [UnityTest]
        public IEnumerator Fitter_SetsTheFieldOfViewOnAPerspectiveCamera()
        {
            _camera.orthographic = false;
            PlayfieldFitter fitter = _rig.AddComponent<PlayfieldFitter>();
            fitter.Configure(new Vector2(16f, 9f), distance: 10f);
            _camera.aspect = 16f / 9f;

            yield return null;
            yield return null;

            Assert.AreEqual(PlayfieldFit.VerticalFieldOfView(new Vector2(16f, 9f), 10f, 16f / 9f, FitMode.Contain), _camera.fieldOfView, 0.01f);
        }
    }
}
