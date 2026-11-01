using NUnit.Framework;
using UnityEngine;

namespace Pretzel.Parallax.Tests
{
    public sealed class ParallaxControllerTests
    {
        private GameObject _cameraObject;
        private GameObject _root;
        private ParallaxController _controller;

        [SetUp]
        public void SetUp()
        {
            _cameraObject = new GameObject("Camera", typeof(Camera));
            _cameraObject.GetComponent<Camera>().orthographic = true;
            _root = new GameObject("Parallax");
            _controller = _root.AddComponent<ParallaxController>();
            _controller.SetCamera(_cameraObject.transform);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_cameraObject);
        }

        private ParallaxLayer AddLayer(Vector3 position, Vector2 factor)
        {
            GameObject layerObject = new("Layer");
            layerObject.transform.SetParent(_root.transform, false);
            layerObject.transform.position = position;
            ParallaxLayer layer = layerObject.AddComponent<ParallaxLayer>();
            layer.Configure(factor);
            return layer;
        }

        [Test]
        public void Layers_MoveByTheirFactorOfTheCameraMovement()
        {
            ParallaxLayer far = AddLayer(new Vector3(0f, 0f, 10f), new Vector2(0.8f, 0f));
            ParallaxLayer near = AddLayer(new Vector3(0f, 0f, 5f), new Vector2(0.2f, 0f));
            _controller.Rebase();

            _cameraObject.transform.position = new Vector3(10f, 3f, 0f);
            _controller.Refresh();

            Assert.AreEqual(8f, far.transform.position.x, 0.0001f);
            Assert.AreEqual(2f, near.transform.position.x, 0.0001f);
            Assert.AreEqual(0f, far.transform.position.y, 0.0001f);
            Assert.AreEqual(10f, far.transform.position.z, 0.0001f);
        }

        [Test]
        public void Refresh_DoesNothingWhenTheCameraDidNotMove()
        {
            AddLayer(Vector3.zero, new Vector2(0.5f, 0.5f));
            _controller.Rebase();
            _cameraObject.transform.position = new Vector3(1f, 0f, 0f);

            Assert.IsTrue(_controller.Refresh());
            Assert.IsFalse(_controller.Refresh());
        }

        [Test]
        public void Rebase_MakesTheCurrentPoseTheStart()
        {
            ParallaxLayer layer = AddLayer(Vector3.zero, new Vector2(0.5f, 0f));
            _controller.Rebase();
            _cameraObject.transform.position = new Vector3(10f, 0f, 0f);
            _controller.Refresh();

            _controller.Rebase();
            _controller.Refresh();

            Assert.AreEqual(5f, layer.transform.position.x, 0.0001f);
        }

        [Test]
        public void ACameraCut_PlacesLayersAsIfTheCameraHadTravelled()
        {
            ParallaxLayer layer = AddLayer(Vector3.zero, new Vector2(0.5f, 0f));
            _controller.Rebase();

            _cameraObject.transform.position = new Vector3(100f, 0f, 0f);
            _controller.Refresh();
            _cameraObject.transform.position = Vector3.zero;
            _controller.Refresh();

            Assert.AreEqual(0f, layer.transform.position.x, 0.0001f);
        }

        [Test]
        public void RepeatingLayer_StaysWithinHalfATileOfTheCamera()
        {
            ParallaxLayer layer = AddLayer(Vector3.zero, new Vector2(0.5f, 0f));
            layer.Configure(new Vector2(0.5f, 0f), repeatX: true, tileSize: new Vector2(8f, 0f));
            _controller.Rebase();

            for (float x = 0f; x <= 200f; x += 0.7f)
            {
                _cameraObject.transform.position = new Vector3(x, 0f, 0f);
                _controller.Refresh();

                Assert.LessOrEqual(Mathf.Abs(layer.transform.position.x - x), 4.0001f, $"Camera at {x}");
            }
        }

        [Test]
        public void SnappingLayer_LandsOnWholePixels()
        {
            ParallaxLayer layer = AddLayer(Vector3.zero, Vector2.zero);
            layer.Configure(new Vector2(0.33f, 0f), snapToPixels: true);
            _controller.SetPixelsPerUnit(16f);
            _controller.Rebase();

            _cameraObject.transform.position = new Vector3(1.234f, 0f, 0f);
            _controller.Refresh();

            float pixels = layer.transform.position.x * 16f;
            Assert.AreEqual(Mathf.Round(pixels), pixels, 0.0001f);
        }

        [Test]
        public void DepthLayer_UnderAnOrthographicCamera_GetsAFactorFromItsDistance()
        {
            _controller.SetFocalDistance(10f);
            ParallaxLayer far = AddLayer(new Vector3(0f, 0f, 40f), Vector2.zero);
            far.ConfigureDepth(new Vector2(1f, 0f));
            ParallaxLayer focal = AddLayer(new Vector3(0f, 0f, 10f), Vector2.zero);
            focal.ConfigureDepth(new Vector2(1f, 0f));
            ParallaxLayer near = AddLayer(new Vector3(0f, 0f, 5f), Vector2.zero);
            near.ConfigureDepth(new Vector2(1f, 0f));

            _controller.Rebase();

            Assert.AreEqual(0.75f, far.Factor.x, 0.0001f);
            Assert.AreEqual(0f, focal.Factor.x, 0.0001f);
            Assert.AreEqual(-1f, near.Factor.x, 0.0001f);
            Assert.AreEqual(0f, far.Factor.y, 0.0001f);
        }

        [Test]
        public void DepthLayer_UnderAPerspectiveCamera_IsLeftToTheProjection()
        {
            _cameraObject.GetComponent<Camera>().orthographic = false;
            ParallaxLayer far = AddLayer(new Vector3(0f, 0f, 40f), Vector2.zero);
            far.ConfigureDepth(Vector2.one);

            _controller.Rebase();

            Assert.AreEqual(Vector2.zero, far.Factor);
        }

        [Test]
        public void TileSize_FallsBackToTheSpriteSize()
        {
            ParallaxLayer layer = AddLayer(Vector3.zero, Vector2.zero);
            layer.transform.localScale = new Vector3(2f, 2f, 1f);
            SpriteRenderer spriteRenderer = layer.gameObject.AddComponent<SpriteRenderer>();
            Texture2D texture = new(64, 32);
            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 64, 32), new Vector2(0.5f, 0.5f), 16f);
            layer.Configure(Vector2.zero, repeatX: true);

            _controller.Rebase();

            Assert.AreEqual(8f, layer.TileSize.x, 0.0001f);
            Object.DestroyImmediate(spriteRenderer.sprite);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void DestroyedLayers_AreSkipped()
        {
            ParallaxLayer layer = AddLayer(Vector3.zero, new Vector2(0.5f, 0f));
            AddLayer(Vector3.zero, new Vector2(0.5f, 0f));
            _controller.Rebase();
            Object.DestroyImmediate(layer.gameObject);

            _cameraObject.transform.position = new Vector3(2f, 0f, 0f);

            Assert.DoesNotThrow(() => _controller.Refresh());
        }

        [Test]
        public void AutoScroll_MovesALayerWhileTheCameraStandsStill()
        {
            ParallaxLayer layer = AddLayer(Vector3.zero, Vector2.zero);
            layer.SetAutoScroll(new Vector2(0f, -2f));
            _controller.Rebase();

            Assert.IsTrue(_controller.Refresh(0.5f));
            Assert.AreEqual(-1f, layer.transform.position.y, 0.0001f);

            Assert.IsTrue(_controller.Refresh(0.5f));
            Assert.AreEqual(-2f, layer.transform.position.y, 0.0001f);
        }

        [Test]
        public void WithoutAutoScroll_AStillCameraStillMeansNothingMoves()
        {
            AddLayer(Vector3.zero, new Vector2(0.5f, 0f));
            _controller.Rebase();
            _controller.Refresh(0.1f);

            Assert.IsFalse(_controller.Refresh(0.1f));
        }

        [Test]
        public void AutoScroll_AddsToTheCameraParallax()
        {
            ParallaxLayer layer = AddLayer(Vector3.zero, new Vector2(0.5f, 0f));
            layer.SetAutoScroll(new Vector2(1f, 0f));
            _controller.Rebase();

            _cameraObject.transform.position = new Vector3(4f, 0f, 0f);
            _controller.Refresh(2f);

            Assert.AreEqual(2f + 2f, layer.transform.position.x, 0.0001f);
        }

        [Test]
        public void AutoScrollScale_MultipliesTheSpeed()
        {
            ParallaxLayer layer = AddLayer(Vector3.zero, Vector2.zero);
            layer.SetAutoScroll(new Vector2(0f, -1f));
            _controller.SetAutoScrollScale(3f);
            _controller.Rebase();

            _controller.Refresh(1f);

            Assert.AreEqual(-3f, layer.transform.position.y, 0.0001f);
        }

        [Test]
        public void ARepeatingLayer_KeepsItsScrollOffsetWithinOneTile_AndStaysNearTheCamera()
        {
            ParallaxLayer layer = AddLayer(Vector3.zero, Vector2.zero);
            layer.Configure(Vector2.zero, repeatY: true, tileSize: new Vector2(0f, 4f));
            layer.SetAutoScroll(new Vector2(0f, -3f));
            _controller.Rebase();

            for (int i = 0; i < 500; i++)
            {
                _controller.Refresh(0.1f);

                Assert.LessOrEqual(Mathf.Abs(layer.ScrollOffset.y), 4f);
                Assert.LessOrEqual(Mathf.Abs(layer.transform.position.y), 2.0001f, $"Step {i}");
            }
        }

        [Test]
        public void ZeroAutoScroll_IsNotScrolling()
        {
            ParallaxLayer layer = AddLayer(Vector3.zero, Vector2.zero);

            Assert.IsFalse(layer.IsScrolling);
            layer.SetAutoScroll(new Vector2(0f, 1f));
            Assert.IsTrue(layer.IsScrolling);
        }

        [Test]
        public void Rebase_ClearsTheScrollOffset()
        {
            ParallaxLayer layer = AddLayer(Vector3.zero, Vector2.zero);
            layer.SetAutoScroll(new Vector2(1f, 0f));
            _controller.Rebase();
            _controller.Refresh(3f);

            _controller.Rebase();

            Assert.AreEqual(Vector2.zero, layer.ScrollOffset);
        }
    }
}
