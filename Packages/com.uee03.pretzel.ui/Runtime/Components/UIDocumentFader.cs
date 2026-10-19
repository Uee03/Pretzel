using Pretzel.Tween;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.UI
{
    /// <summary>
    /// Fades one element of the <see cref="UIDocument"/> on this object, or the whole document, with USS transitions.
    /// To fade in from invisible, give the element <c>opacity: 0</c> in its USS. Fading rebinds after the document rebuilds its tree.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Pretzel/UI/UI Document Fader")]
    public class UIDocumentFader : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Name of the element to fade. Leave empty to fade the whole document.")]
        [SerializeField] private string _elementName;

        [Header("Fade On Start")]
        [SerializeField] private bool _fadeOnStart;
        [SerializeField] private FadeType _fadeOnStartType = FadeType.FadeIn;

        [Header("Fade In")]
        [SerializeField] private EaseType _fadeInEase = EaseType.Linear;
        [SerializeField, Min(0f)] private float _fadeInDuration = UIToolkitFader.DefaultDuration;

        [Header("Fade Out")]
        [SerializeField] private EaseType _fadeOutEase = EaseType.Linear;
        [SerializeField, Min(0f)] private float _fadeOutDuration = UIToolkitFader.DefaultDuration;

        private UIDocument _document;
        private bool _warned;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void Start()
        {
            if (_fadeOnStart)
            {
                Fade(_fadeOnStartType);
            }
        }

        private void OnDisable()
        {
            if (TryGetFader(out UIToolkitFader fader))
            {
                fader.Cancel();
            }
        }

        public void Fade(FadeType type)
        {
            switch (type)
            {
                case FadeType.FadeIn:
                    FadeIn();
                    break;
                case FadeType.FadeOut:
                    FadeOut();
                    break;
                case FadeType.FadeInNow:
                    FadeInNow();
                    break;
                case FadeType.FadeOutNow:
                    FadeOutNow();
                    break;
            }
        }

        public void FadeIn()
        {
            if (TryGetFader(out UIToolkitFader fader))
            {
                fader.FadeIn(_fadeInDuration, _fadeInEase);
            }
        }

        public void FadeOut()
        {
            if (TryGetFader(out UIToolkitFader fader))
            {
                fader.FadeOut(_fadeOutDuration, _fadeOutEase);
            }
        }

        public void FadeInNow()
        {
            if (TryGetFader(out UIToolkitFader fader))
            {
                fader.FadeInNow();
            }
        }

        public void FadeOutNow()
        {
            if (TryGetFader(out UIToolkitFader fader))
            {
                fader.FadeOutNow();
            }
        }

        private bool TryGetFader(out UIToolkitFader fader)
        {
            fader = null;
            VisualElement root = _document ? _document.rootVisualElement : null;
            VisualElement target = string.IsNullOrEmpty(_elementName) ? root : root?.Q(_elementName);
            if (target == null)
            {
                if (!_warned && root != null)
                {
                    _warned = true;
                    Debug.LogWarning($"[Pretzel.UI] '{name}': no element named '{_elementName}' in the document.", this);
                }

                return false;
            }

            fader = target.GetFader();
            return true;
        }
    }
}
