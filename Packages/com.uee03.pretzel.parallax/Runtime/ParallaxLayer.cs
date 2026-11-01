using UnityEngine;

namespace Pretzel.Parallax
{
    /// <summary>
    /// One scrolling layer. It holds settings only; a <see cref="ParallaxController"/> moves it, so a scene with many
    /// layers still runs one update. Place the layer where it should be when the camera is at its starting position.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("Manual uses the factor below. Depth derives it from the distance to the camera.")]
        [SerializeField] private ParallaxMode _mode = ParallaxMode.Manual;

        [Tooltip("How much the layer follows the camera on each axis. 0 stays in the world, 1 stays on screen, in between is farther away.")]
        [SerializeField] private Vector2 _factor = new(0.5f, 0.5f);

        [Tooltip("Depth mode only: which axes the depth factor applies to. Use (1, 0) for a side scroller.")]
        [SerializeField] private Vector2 _depthAxes = Vector2.one;

        [Tooltip("Repeat the layer endlessly on the horizontal axis.")]
        [SerializeField] private bool _repeatX;

        [Tooltip("Repeat the layer endlessly on the vertical axis.")]
        [SerializeField] private bool _repeatY;

        [Tooltip("World size of one repeating tile. 0 uses the size of the SpriteRenderer's sprite.")]
        [SerializeField] private Vector2 _tileSize = Vector2.zero;

        [Tooltip("Round the position to whole pixels so pixel art does not shimmer. Needs Pixels Per Unit on the controller.")]
        [SerializeField] private bool _snapToPixels;

        [Tooltip("World units per second the layer scrolls by itself, on top of the camera. For a backdrop behind a camera that does not move.")]
        [SerializeField] private Vector2 _autoScroll;

        private Vector3 _origin;
        private Vector2 _resolvedFactor;
        private Vector2 _resolvedTile;
        private Vector2 _scrollOffset;

        public ParallaxMode Mode => _mode;

        /// <summary>The factor in use, which for Depth mode is computed when the controller captures the layer.</summary>
        public Vector2 Factor => _resolvedFactor;

        public Vector2 TileSize => _resolvedTile;

        public bool RepeatX => _repeatX;

        public bool RepeatY => _repeatY;

        /// <summary>World units per second the layer scrolls by itself.</summary>
        public Vector2 AutoScroll => _autoScroll;

        /// <summary>How far the automatic scroll has moved the layer. Kept within one tile on a repeating axis.</summary>
        public Vector2 ScrollOffset => _scrollOffset;

        public bool IsScrolling => _autoScroll.x != 0f || _autoScroll.y != 0f;

        /// <summary>Changes the automatic scroll while the game runs. It takes effect on the next controller update.</summary>
        public void SetAutoScroll(Vector2 unitsPerSecond)
        {
            _autoScroll = unitsPerSecond;
        }

        /// <summary>Sets the layer from code. Call <see cref="ParallaxController.Rebase"/> afterwards.</summary>
        public void Configure(Vector2 factor, bool repeatX = false, bool repeatY = false, Vector2 tileSize = default, bool snapToPixels = false)
        {
            _mode = ParallaxMode.Manual;
            _factor = factor;
            _repeatX = repeatX;
            _repeatY = repeatY;
            _tileSize = tileSize;
            _snapToPixels = snapToPixels;
        }

        /// <summary>Sets a depth layer from code. Call <see cref="ParallaxController.Rebase"/> afterwards.</summary>
        public void ConfigureDepth(Vector2 depthAxes, bool repeatX = false, bool repeatY = false, Vector2 tileSize = default)
        {
            _mode = ParallaxMode.Depth;
            _depthAxes = depthAxes;
            _repeatX = repeatX;
            _repeatY = repeatY;
            _tileSize = tileSize;
        }

        internal void Capture(Transform camera, bool orthographic, float focalDistance)
        {
            _origin = transform.position;
            _resolvedFactor = ResolveFactor(camera, orthographic, focalDistance);
            _resolvedTile = ResolveTileSize();
            _scrollOffset = Vector2.zero;
        }

        internal void Advance(float deltaTime, float scale)
        {
            _scrollOffset += _autoScroll * (deltaTime * scale);

            // Wrapping the offset by whole tiles looks the same and stops the number growing for ever.
            if (_repeatX && _resolvedTile.x > Mathf.Epsilon)
            {
                _scrollOffset.x %= _resolvedTile.x;
            }

            if (_repeatY && _resolvedTile.y > Mathf.Epsilon)
            {
                _scrollOffset.y %= _resolvedTile.y;
            }
        }

        internal void Apply(Vector3 cameraPosition, Vector3 cameraDelta, float pixelUnit)
        {
            Vector3 position = ParallaxMath.Position(_origin, cameraDelta, _resolvedFactor);
            position.x += _scrollOffset.x;
            position.y += _scrollOffset.y;
            if (_repeatX)
            {
                position.x = ParallaxMath.Wrap(cameraPosition.x, position.x, _resolvedTile.x);
            }

            if (_repeatY)
            {
                position.y = ParallaxMath.Wrap(cameraPosition.y, position.y, _resolvedTile.y);
            }

            if (_snapToPixels)
            {
                position.x = ParallaxMath.Snap(position.x, pixelUnit);
                position.y = ParallaxMath.Snap(position.y, pixelUnit);
            }

            transform.position = position;
        }

        private Vector2 ResolveFactor(Transform camera, bool orthographic, float focalDistance)
        {
            if (_mode == ParallaxMode.Manual)
            {
                return _factor;
            }

            if (!orthographic)
            {
                return Vector2.zero;
            }

            float distance = Vector3.Dot(_origin - camera.position, camera.forward);
            float depthFactor = ParallaxMath.FactorFromDepth(distance, focalDistance);
            return new Vector2(depthFactor * _depthAxes.x, depthFactor * _depthAxes.y);
        }

        private Vector2 ResolveTileSize()
        {
            Vector2 tile = _tileSize;
            if ((tile.x <= 0f && _repeatX) || (tile.y <= 0f && _repeatY))
            {
                SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    Vector3 size = spriteRenderer.sprite.bounds.size;
                    Vector3 scale = transform.lossyScale;
                    tile.x = tile.x > 0f ? tile.x : Mathf.Abs(size.x * scale.x);
                    tile.y = tile.y > 0f ? tile.y : Mathf.Abs(size.y * scale.y);
                }
            }

            return tile;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector2 tile = Application.isPlaying ? _resolvedTile : _tileSize;
            if (tile.x <= 0f && tile.y <= 0f)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.8f);
            Vector3 size = new(tile.x > 0f ? tile.x : 0.1f, tile.y > 0f ? tile.y : 0.1f, 0f);
            Gizmos.DrawWireCube(transform.position, size);
        }
#endif
    }
}
