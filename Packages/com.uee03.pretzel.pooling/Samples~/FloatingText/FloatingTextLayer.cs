using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Pretzel.Samples.Pooling.FloatingText
{
    /// <summary>
    /// Floating damage numbers on a UI Toolkit panel. Labels come from an <see cref="ObjectPool{T}"/> and the per-number animation
    /// state from a second one, so showing a number allocates nothing once the pools are warm. Elements are animated with
    /// <c>translate</c> and <c>opacity</c>, which do not trigger a layout pass.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class FloatingTextLayer : MonoBehaviour
    {
        private const string ClassName = "floating-text";

        [SerializeField] private UIDocument _document;

        [Tooltip("Optional. Assign FloatingText.uss for the sample look.")]
        [SerializeField] private StyleSheet _styleSheet;

        [SerializeField, Min(0.1f)] private float _lifetime = 0.9f;
        [SerializeField, Min(0f)] private float _rise = 70f;
        [SerializeField, Min(1)] private int _maxActive = 120;

        private readonly List<Entry> _active = new();
        private ObjectPool<Label> _labels;
        private ObjectPool<Entry> _entries;
        private VisualElement _layer;

        public int ActiveCount => _active.Count;

        public int PooledLabelsCreated => _labels?.CountAll ?? 0;

        public int PooledLabelsIdle => _labels?.CountInactive ?? 0;

        /// <summary>The size of the panel in pixels, or a default before the first layout pass.</summary>
        public Vector2 PanelSize
        {
            get
            {
                Rect bounds = _document.rootVisualElement.contentRect;
                return bounds.width > 1f && bounds.height > 1f ? bounds.size : new Vector2(800f, 600f);
            }
        }

        private void Reset()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (!_document)
            {
                _document = GetComponent<UIDocument>();
            }

            VisualElement root = _document.rootVisualElement;
            if (_styleSheet)
            {
                root.styleSheets.Add(_styleSheet);
            }

            _layer = new VisualElement { pickingMode = PickingMode.Ignore };
            _layer.style.position = Position.Absolute;
            _layer.style.left = 0f;
            _layer.style.top = 0f;
            _layer.style.right = 0f;
            _layer.style.bottom = 0f;
            root.Add(_layer);

            _labels = new ObjectPool<Label>(CreateLabel, OnGetLabel, OnReleaseLabel, OnDestroyLabel, true, 32, 128);
            _entries = new ObjectPool<Entry>(() => new Entry(), null, entry => entry.Label = null, null, true, 32, 128);
        }

        private void OnDisable()
        {
            _active.Clear();
            _labels?.Dispose();
            _entries?.Dispose();
            _labels = null;
            _entries = null;
            _layer?.RemoveFromHierarchy();
            _layer = null;
        }

        private void Update()
        {
            float delta = Time.unscaledDeltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Entry entry = _active[i];
                entry.Age += delta;
                float t = entry.Age / _lifetime;
                if (t >= 1f)
                {
                    Remove(i);
                    continue;
                }

                // Ease out: fast at first, slowing as it fades.
                float eased = 1f - (1f - t) * (1f - t);
                entry.Label.style.translate = new Translate(0f, -_rise * eased);
                entry.Label.style.opacity = 1f - t * t;
            }
        }

        /// <summary>Shows a number at a position in panel pixels. The oldest number is recycled when the cap is reached.</summary>
        public void Show(string text, Vector2 position, Color color)
        {
            if (_layer == null)
            {
                return;
            }

            if (_active.Count >= _maxActive)
            {
                Remove(0);
            }

            Label label = _labels.Get();
            label.text = text;
            label.style.left = position.x;
            label.style.top = position.y;
            label.style.color = color;
            label.style.translate = new Translate(0f, 0f);
            label.style.opacity = 1f;

            Entry entry = _entries.Get();
            entry.Label = label;
            entry.Age = 0f;
            _active.Add(entry);
        }

        private void Remove(int index)
        {
            Entry entry = _active[index];
            _active.RemoveAt(index);
            _labels.Release(entry.Label);
            _entries.Release(entry);
        }

        private Label CreateLabel()
        {
            Label label = new() { pickingMode = PickingMode.Ignore };
            label.AddToClassList(ClassName);
            label.style.position = Position.Absolute;
            _layer.Add(label);
            return label;
        }

        // display none removes the element from layout and rendering, and is cheaper than rebuilding it.
        private static void OnGetLabel(Label label) => label.style.display = DisplayStyle.Flex;

        private static void OnReleaseLabel(Label label) => label.style.display = DisplayStyle.None;

        private static void OnDestroyLabel(Label label) => label.RemoveFromHierarchy();

        private sealed class Entry
        {
            public Label Label;
            public float Age;
        }
    }
}
