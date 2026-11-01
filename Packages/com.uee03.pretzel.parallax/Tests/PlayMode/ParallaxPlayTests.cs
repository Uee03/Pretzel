using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Pretzel.Parallax.PlayTests
{
    public sealed class ParallaxPlayTests
    {
        private GameObject _cameraObject;
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_root);
            Object.Destroy(_cameraObject);
        }

        [Test]
        public void TheController_RunsAfterTheDefaultOrder()
        {
            DefaultExecutionOrder order = typeof(ParallaxController).GetCustomAttribute<DefaultExecutionOrder>();

            Assert.IsNotNull(order);
            Assert.Greater(order.order, 0, "The Cinemachine Brain uses the default order; layers must run after it.");
        }

        [UnityTest]
        public IEnumerator Layers_FollowACameraMovedInUpdate_WithoutLagging()
        {
            _cameraObject = new GameObject("Camera", typeof(Camera));
            _cameraObject.GetComponent<Camera>().orthographic = true;
            CameraMover mover = _cameraObject.AddComponent<CameraMover>();

            _root = new GameObject("Parallax");
            _root.SetActive(false);
            ParallaxController controller = _root.AddComponent<ParallaxController>();
            controller.SetCamera(_cameraObject.transform);
            GameObject layerObject = new("Layer");
            layerObject.transform.SetParent(_root.transform, false);
            ParallaxLayer layer = layerObject.AddComponent<ParallaxLayer>();
            layer.Configure(new Vector2(0.5f, 0f));
            _root.SetActive(true);
            LateRecorder recorder = _root.AddComponent<LateRecorder>();
            recorder.Camera = _cameraObject.transform;
            recorder.Layer = layerObject.transform;
            yield return null;

            for (int frame = 0; frame < 5; frame++)
            {
                int samples = recorder.Samples;
                while (recorder.Samples == samples)
                {
                    yield return null;
                }

                Assert.AreEqual(recorder.CameraX * 0.5f, recorder.LayerX, 0.0001f, $"Frame {frame}");
            }

            Assert.Greater(mover.Step, 0f);
        }

        [UnityTest]
        public IEnumerator AutoScroll_RunsInTheGameLoop_AndStopsWhenTimeStops()
        {
            _cameraObject = new GameObject("Camera", typeof(Camera));
            _root = new GameObject("Parallax");
            _root.SetActive(false);
            ParallaxController controller = _root.AddComponent<ParallaxController>();
            controller.SetCamera(_cameraObject.transform);
            GameObject layerObject = new("Layer");
            layerObject.transform.SetParent(_root.transform, false);
            ParallaxLayer layer = layerObject.AddComponent<ParallaxLayer>();
            layer.Configure(Vector2.zero);
            layer.SetAutoScroll(new Vector2(0f, -5f));
            _root.SetActive(true);
            yield return null;

            float waited = 0f;
            while (layerObject.transform.position.y > -0.2f && waited < 3f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.Less(layerObject.transform.position.y, -0.2f, "The layer did not scroll on its own.");

            Time.timeScale = 0f;
            try
            {
                yield return null;
                float frozen = layerObject.transform.position.y;
                yield return null;
                yield return null;

                Assert.AreEqual(frozen, layerObject.transform.position.y, 0.0001f, "A backdrop must stop with the game.");
            }
            finally
            {
                Time.timeScale = 1f;
            }
        }

        [UnityTest]
        public IEnumerator DisablingTheController_StopsTheLayers()
        {
            _cameraObject = new GameObject("Camera", typeof(Camera));
            CameraMover mover = _cameraObject.AddComponent<CameraMover>();
            _root = new GameObject("Parallax");
            _root.SetActive(false);
            ParallaxController controller = _root.AddComponent<ParallaxController>();
            controller.SetCamera(_cameraObject.transform);
            GameObject layerObject = new("Layer");
            layerObject.transform.SetParent(_root.transform, false);
            layerObject.AddComponent<ParallaxLayer>().Configure(new Vector2(0.5f, 0f));
            _root.SetActive(true);
            yield return null;

            controller.enabled = false;
            float frozen = layerObject.transform.position.x;
            yield return null;
            yield return null;

            Assert.AreEqual(frozen, layerObject.transform.position.x, 0.0001f);
            Assert.Greater(mover.Step, 0f);
        }
    }
}
