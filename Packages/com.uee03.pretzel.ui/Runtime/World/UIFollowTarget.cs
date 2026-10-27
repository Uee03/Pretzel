using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.UI
{
    /// <summary>
    /// Keeps an element of a screen-space <see cref="UIDocument"/> over a world target, such as a health bar or a name tag.
    /// The element is positioned absolutely with <c>translate</c>, which does not trigger a layout pass.
    /// </summary>
    [AddComponentMenu("Pretzel/UI/UI Follow Target")]
    public sealed class UIFollowTarget : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;
        [Tooltip("Name of the element to move. Empty moves the document root.")]
        [SerializeField] private string _elementName;
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _worldOffset = new(0f, 2f, 0f);
        [Tooltip("Point of the element placed on the target. (0.5, 1) is bottom center.")]
        [SerializeField] private Vector2 _pivot = new(0.5f, 1f);
        [Tooltip("Defaults to Camera.main, looked up once.")]
        [SerializeField] private Camera _camera;
        [SerializeField] private bool _hideWhenOffscreen = true;

        private VisualElement _element;
        private bool _explicitElement;

        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        public VisualElement Element => _element;

        /// <summary>Sets everything from code, for elements created at runtime.</summary>
        public void Configure(UIDocument document, string elementName, Transform target, Camera camera = null)
        {
            _document = document;
            _elementName = elementName;
            _target = target;
            if (camera)
            {
                _camera = camera;
            }

            _element = null;
            _explicitElement = false;
        }

        /// <summary>Follows an element created at runtime. The element must be in the document's tree.</summary>
        public void Configure(UIDocument document, VisualElement element, Transform target, Camera camera = null)
        {
            Configure(document, element?.name, target, camera);
            _element = element;
            _explicitElement = element != null;
            if (_element != null)
            {
                MakeAbsolute(_element);
            }
        }

        private void LateUpdate()
        {
            if (!_target || !_document || !ResolveElement() || !ResolveCamera())
            {
                return;
            }

            Vector3 world = _target.position + _worldOffset;
            bool visible = WorldToPanel.TryGetPanelPosition(_element.panel, _camera, world, out Vector2 position)
                           && (!_hideWhenOffscreen || WorldToPanel.IsOnScreen(_camera, world, 0.1f));
            _element.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
            if (!visible)
            {
                return;
            }

            Rect layout = _element.layout;
            float width = float.IsNaN(layout.width) ? 0f : layout.width;
            float height = float.IsNaN(layout.height) ? 0f : layout.height;
            _element.style.translate = new Translate(position.x - width * _pivot.x, position.y - height * _pivot.y);
        }

        private bool ResolveElement()
        {
            if (_element != null && _element.panel != null)
            {
                return true;
            }

            // An element assigned from code is not looked up again; a rebuilt document needs a new Configure call.
            if (_explicitElement)
            {
                return false;
            }

            VisualElement root = _document.rootVisualElement;
            _element = string.IsNullOrEmpty(_elementName) ? root : root?.Q(_elementName);
            if (_element == null || _element.panel == null)
            {
                _element = null;
                return false;
            }

            MakeAbsolute(_element);
            return true;
        }

        private static void MakeAbsolute(VisualElement element)
        {
            element.style.position = Position.Absolute;
            element.style.left = 0;
            element.style.top = 0;
        }

        private bool ResolveCamera()
        {
            if (!_camera)
            {
                _camera = Camera.main;
            }

            return _camera;
        }
    }
}
