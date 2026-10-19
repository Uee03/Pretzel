using Pretzel.Tween;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.UI
{
    /// <summary>
    /// Fades an element in while the pointer is over a trigger element, and out when it leaves.
    /// Callbacks are registered on enable and removed on disable.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Pretzel/UI/UI Hover Fade")]
    public class UIHoverFade : MonoBehaviour
    {
        [Header("Elements")]
        [Tooltip("Name of the element that reacts to the pointer. Leave empty to use the whole document.")]
        [SerializeField] private string _hoverElementName;
        [Tooltip("Name of the element to fade. Leave empty to fade the hover element itself.")]
        [SerializeField] private string _fadeElementName;
        [Tooltip("Fade the target out as soon as the component binds, so it only appears on hover.")]
        [SerializeField] private bool _startHidden;

        [Header("Fade")]
        [SerializeField] private EaseType _fadeInEase = EaseType.OutQuad;
        [SerializeField, Min(0f)] private float _fadeInDuration = 0.15f;
        [SerializeField] private EaseType _fadeOutEase = EaseType.InQuad;
        [SerializeField, Min(0f)] private float _fadeOutDuration = 0.15f;

        private UIDocument _document;
        private VisualElement _hoverElement;
        private UIToolkitFader _fader;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            TryBind();
        }

        // UIDocument may build its tree after this component's OnEnable, so try again once every OnEnable has run.
        private void Start()
        {
            TryBind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void TryBind()
        {
            if (_hoverElement != null || !_document)
            {
                return;
            }

            VisualElement root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            VisualElement hover = string.IsNullOrEmpty(_hoverElementName) ? root : root.Q(_hoverElementName);
            VisualElement fade = string.IsNullOrEmpty(_fadeElementName) ? hover : root.Q(_fadeElementName);
            if (hover == null || fade == null)
            {
                Debug.LogWarning($"[Pretzel.UI] '{name}': could not find the hover or fade element in the document.", this);
                return;
            }

            _hoverElement = hover;
            _fader = fade.GetFader();

            // A hidden element gets no pointer events, so only hide it when the trigger is not hidden along with it.
            _fader.HideOnFadeOut = fade != hover && !fade.Contains(hover);

            hover.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            hover.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);

            if (_startHidden)
            {
                _fader.FadeOutNow();
            }
        }

        private void Unbind()
        {
            if (_hoverElement == null)
            {
                return;
            }

            _hoverElement.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
            _hoverElement.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
            _fader?.Cancel();
            _hoverElement = null;
            _fader = null;
        }

        private void OnPointerEnter(PointerEnterEvent evt)
        {
            _fader.FadeIn(_fadeInDuration, _fadeInEase);
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            _fader.FadeOut(_fadeOutDuration, _fadeOutEase);
        }
    }
}
