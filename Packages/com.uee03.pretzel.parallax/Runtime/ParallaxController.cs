using System.Collections.Generic;
using UnityEngine;

namespace Pretzel.Parallax
{
    /// <summary>
    /// Moves every layer from one LateUpdate, after Cinemachine has placed the camera, without allocating. Does nothing
    /// in a frame where the camera did not move.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    public sealed class ParallaxController : MonoBehaviour
    {
        /// <summary>Later than the Cinemachine Brain, which uses the default order, so layers follow the camera's final position.</summary>
        public const int ExecutionOrder = 300;

        [Tooltip("The camera to follow. Empty uses the main camera. With Cinemachine, use the real camera, not a virtual one.")]
        [SerializeField] private Transform _camera;

        [Tooltip("Depth mode: the distance from the camera at which a layer does not move. Layers nearer than this move faster than the world.")]
        [SerializeField, Min(0.01f)] private float _focalDistance = 10f;

        [Tooltip("Sprite pixels per world unit, for layers that snap to pixels. 0 disables snapping.")]
        [SerializeField, Min(0f)] private float _pixelsPerUnit;

        [Tooltip("Also use every ParallaxLayer on this object's children.")]
        [SerializeField] private bool _includeChildren = true;

        [SerializeField] private List<ParallaxLayer> _layers = new();

        [Tooltip("Multiplies every layer's automatic scroll, for example to speed a backdrop up as a game gets harder.")]
        [SerializeField, Min(0f)] private float _autoScrollScale = 1f;

        private readonly List<ParallaxLayer> _active = new();
        private Camera _cameraComponent;
        private Vector3 _cameraOrigin;
        private Vector3 _lastCameraPosition;
        private bool _applied;

        public Transform CameraTransform => _camera;

        public float FocalDistance => _focalDistance;

        public float AutoScrollScale => _autoScrollScale;

        public IReadOnlyList<ParallaxLayer> Layers => _active;

        /// <summary>Changes the multiplier on every layer's automatic scroll. It takes effect at once.</summary>
        public void SetAutoScrollScale(float scale)
        {
            _autoScrollScale = Mathf.Max(0f, scale);
        }

        public void SetCamera(Transform camera)
        {
            _camera = camera;
        }

        public void SetFocalDistance(float focalDistance)
        {
            _focalDistance = Mathf.Max(0.01f, focalDistance);
        }

        public void SetPixelsPerUnit(float pixelsPerUnit)
        {
            _pixelsPerUnit = Mathf.Max(0f, pixelsPerUnit);
        }

        /// <summary>
        /// Makes the layers' and the camera's current positions the starting pose. Call it after changing a layer,
        /// the camera type or the focal distance. It does not move anything, so it is safe in the middle of a game.
        /// </summary>
        [ContextMenu("Rebase")]
        public void Rebase()
        {
            ResolveCamera();
            CollectLayers();
            if (_camera == null)
            {
                return;
            }

            bool orthographic = _cameraComponent == null || _cameraComponent.orthographic;
            _cameraOrigin = _camera.position;
            foreach (ParallaxLayer layer in _active)
            {
                layer.Capture(_camera, orthographic, _focalDistance);
            }

            _applied = false;
        }

        /// <summary>Moves the layers for the camera's current position. Returns false when nothing needed to move.</summary>
        public bool Refresh()
        {
            return Refresh(0f);
        }

        /// <summary>
        /// Moves the layers for the camera's current position and advances every auto-scrolling layer by
        /// <paramref name="deltaTime"/> (scaled by <see cref="AutoScrollScale"/>). Returns false when nothing moved.
        /// </summary>
        public bool Refresh(float deltaTime)
        {
            if (_camera == null)
            {
                return false;
            }

            bool scrolled = false;
            if (deltaTime > 0f)
            {
                for (int i = 0; i < _active.Count; i++)
                {
                    ParallaxLayer layer = _active[i];
                    if (layer != null && layer.IsScrolling)
                    {
                        layer.Advance(deltaTime, _autoScrollScale);
                        scrolled = true;
                    }
                }
            }

            Vector3 cameraPosition = _camera.position;
            if (_applied && !scrolled && cameraPosition == _lastCameraPosition)
            {
                return false;
            }

            Vector3 delta = cameraPosition - _cameraOrigin;
            float pixelUnit = _pixelsPerUnit > 0f ? 1f / _pixelsPerUnit : 0f;
            for (int i = 0; i < _active.Count; i++)
            {
                ParallaxLayer layer = _active[i];
                if (layer != null)
                {
                    layer.Apply(cameraPosition, delta, pixelUnit);
                }
            }

            _lastCameraPosition = cameraPosition;
            _applied = true;
            return true;
        }

        private void OnEnable()
        {
            Rebase();
            Refresh();
        }

        private void LateUpdate()
        {
            Refresh(Time.deltaTime);
        }

        private void ResolveCamera()
        {
            if (_camera == null)
            {
                Camera main = Camera.main;
                if (main == null)
                {
                    Debug.LogError("ParallaxController needs a camera: assign one or tag a camera MainCamera.", this);
                    _cameraComponent = null;
                    return;
                }

                _camera = main.transform;
            }

            _cameraComponent = _camera.GetComponent<Camera>();
        }

        private void CollectLayers()
        {
            _active.Clear();
            foreach (ParallaxLayer layer in _layers)
            {
                if (layer != null && !_active.Contains(layer))
                {
                    _active.Add(layer);
                }
            }

            if (_includeChildren)
            {
                foreach (ParallaxLayer layer in GetComponentsInChildren<ParallaxLayer>(true))
                {
                    if (!_active.Contains(layer))
                    {
                        _active.Add(layer);
                    }
                }
            }
        }
    }
}
