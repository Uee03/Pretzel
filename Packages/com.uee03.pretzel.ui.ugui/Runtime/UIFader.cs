using System.Collections;
using System.Collections.Generic;
using Pretzel.Tween;
using UnityEngine;

namespace Pretzel.UI.UGUI
{
    /// <summary>
    /// Fades a <see cref="CanvasGroup"/>, or a list of <see cref="CanvasRenderer"/>s, with an <see cref="EaseType"/> or an <see cref="AnimationCurve"/>.
    /// </summary>
    [AddComponentMenu("Pretzel/UI/UI Fader")]
    public class UIFader : MonoBehaviour
    {
        [Header("Fader Properties")]
        [SerializeField] private bool _useCanvasGroup = true;

        [Header("Fade Components")]
        [Tooltip("Found on this object when empty.")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private List<CanvasRenderer> _uiElements = new();

        [Header("Fade On Start")]
        [SerializeField] private bool _fadeOnStart;
        [SerializeField] private FadeType _fadeOnStartType = FadeType.FadeIn;

        [Header("Fade Properties")]
        [SerializeField] private bool _useEaseType = true;
        [SerializeField] private EaseType _fadeInEaseType = EaseType.Linear;
        [SerializeField] private EaseType _fadeOutEaseType = EaseType.Linear;
        [SerializeField] private AnimationCurve _fadeInAnimationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve _fadeOutAnimationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField, Min(0f)] private float _fadeInDuration = 1f;
        [SerializeField, Min(0f)] private float _fadeOutDuration = 1f;

        private float _alpha = 1f;
        private Coroutine _fadeRoutine;
        private bool _warned;

        public float FadeInDuration
        {
            get => _fadeInDuration;
            set => _fadeInDuration = Mathf.Max(0f, value);
        }

        public float FadeOutDuration
        {
            get => _fadeOutDuration;
            set => _fadeOutDuration = Mathf.Max(0f, value);
        }

        /// <summary>The current alpha of the faded targets.</summary>
        public float Alpha => GetAlpha();

        private void Reset()
        {
            TryGetComponent(out _canvasGroup);
        }

        private void Awake()
        {
            if (_useCanvasGroup && !_canvasGroup)
            {
                TryGetComponent(out _canvasGroup);
            }

            _alpha = ReadAlpha();
        }

        private void Start()
        {
            if (!_fadeOnStart)
            {
                return;
            }

            switch (_fadeOnStartType)
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

        private void OnDisable()
        {
            // Disabling only the component does not stop its coroutines.
            StopFade();
        }

        public void FadeIn()
        {
            StartFade(1f, _fadeInDuration, _fadeInEaseType, _fadeInAnimationCurve);
        }

        public void FadeOut()
        {
            StartFade(0f, _fadeOutDuration, _fadeOutEaseType, _fadeOutAnimationCurve);
        }

        public void FadeInNow()
        {
            StopFade();
            SetAlpha(1f);
        }

        public void FadeOutNow()
        {
            StopFade();
            SetAlpha(0f);
        }

        private void StartFade(float to, float duration, EaseType ease, AnimationCurve curve)
        {
            StopFade();
            if (!HasTargets())
            {
                return;
            }

            if (duration <= 0f || !isActiveAndEnabled)
            {
                SetAlpha(to);
                return;
            }

            _fadeRoutine = StartCoroutine(FadeRoutine(GetAlpha(), to, duration, ease, curve));
        }

        private IEnumerator FadeRoutine(float from, float to, float duration, EaseType ease, AnimationCurve curve)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                SetAlpha(Evaluate(ease, curve, from, to, elapsed / duration));
                yield return null;
                elapsed += Time.deltaTime;
            }

            SetAlpha(to);
            _fadeRoutine = null;
        }

        private float Evaluate(EaseType ease, AnimationCurve curve, float from, float to, float time)
        {
            if (_useEaseType)
            {
                return EaseUtility.Lerp(ease, from, to, time);
            }

            // An empty curve would return 0, so treat it as linear.
            return curve != null && curve.length > 0
                ? EaseUtility.Lerp(from, to, time, curve)
                : Mathf.Lerp(from, to, time);
        }

        private void StopFade()
        {
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }
        }

        private bool HasTargets()
        {
            bool ok = _useCanvasGroup ? _canvasGroup : _uiElements != null && _uiElements.Count > 0;
            if (!ok && !_warned)
            {
                _warned = true;
                Debug.LogWarning(_useCanvasGroup
                    ? $"[Pretzel.UI] '{name}': UIFader has no CanvasGroup."
                    : $"[Pretzel.UI] '{name}': UIFader has no CanvasRenderers to fade.", this);
            }

            return ok;
        }

        private float GetAlpha()
        {
            return _useCanvasGroup && _canvasGroup ? _canvasGroup.alpha : _alpha;
        }

        private float ReadAlpha()
        {
            if (_useCanvasGroup)
            {
                return _canvasGroup ? _canvasGroup.alpha : 1f;
            }

            foreach (CanvasRenderer element in _uiElements)
            {
                if (element)
                {
                    return element.GetAlpha();
                }
            }

            return 1f;
        }

        private void SetAlpha(float alpha)
        {
            _alpha = alpha;
            if (_useCanvasGroup)
            {
                if (_canvasGroup)
                {
                    _canvasGroup.alpha = alpha;
                }

                return;
            }

            foreach (CanvasRenderer element in _uiElements)
            {
                if (element)
                {
                    element.SetAlpha(alpha);
                }
            }
        }
    }
}
