using System;
using System.Collections.Generic;
using Pretzel.Core;
using Pretzel.Tween;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.UI
{
    /// <summary>
    /// Fades the opacity of one <see cref="VisualElement"/> with a USS transition.
    /// A fade-out ends by setting <c>display: none</c>, and a fade-in sets <c>display: flex</c> first.
    /// Starting a new fade cancels the running one without calling its callback.
    /// Use <see cref="VisualElementFadeExtensions.GetFader"/> to get the shared instance for an element.
    /// </summary>
    public sealed class UIToolkitFader
    {
        public const float DefaultDuration = 0.25f;

        // A style change on an element that was display: none does not transition, so the target is applied a moment later.
        private const long ShowDelayMs = 30;
        // Transitions never finish on an element that is not rendered, so a timer completes the fade anyway.
        private const long FallbackMarginMs = 150;

        private static readonly StylePropertyName Opacity = new("opacity");

        private readonly VisualElement _element;
        private int _version;
        private bool _listening;
        private Action _onComplete;
        private bool _hideOnComplete;
        private IVisualElementScheduledItem _showTimer;
        private IVisualElementScheduledItem _fallbackTimer;

        public VisualElement Element => _element;

        public bool IsFading { get; private set; }

        /// <summary>When false, a fade-out only reaches opacity 0 and leaves the element displayed, so it can still receive pointer events.</summary>
        public bool HideOnFadeOut { get; set; } = true;

        public UIToolkitFader(VisualElement element)
        {
            _element = Guard.NotNull(element, nameof(element));
        }

        public void Fade(FadeType type, float duration = DefaultDuration, EaseType ease = EaseType.Linear, Action onComplete = null)
        {
            switch (type)
            {
                case FadeType.FadeIn:
                    FadeIn(duration, ease, onComplete);
                    break;
                case FadeType.FadeOut:
                    FadeOut(duration, ease, onComplete);
                    break;
                case FadeType.FadeInNow:
                    FadeInNow();
                    onComplete?.Invoke();
                    break;
                case FadeType.FadeOutNow:
                    FadeOutNow();
                    onComplete?.Invoke();
                    break;
            }
        }

        public void FadeIn(float duration = DefaultDuration, EaseType ease = EaseType.Linear, Action onComplete = null)
        {
            Begin(1f, duration, ease, false, onComplete);
        }

        public void FadeOut(float duration = DefaultDuration, EaseType ease = EaseType.Linear, Action onComplete = null)
        {
            Begin(0f, duration, ease, HideOnFadeOut, onComplete);
        }

        public void FadeInNow()
        {
            Reset();
            ClearTransition();
            _element.style.display = DisplayStyle.Flex;
            _element.style.opacity = 1f;
        }

        public void FadeOutNow()
        {
            Reset();
            ClearTransition();
            _element.style.opacity = 0f;
            if (HideOnFadeOut)
            {
                _element.style.display = DisplayStyle.None;
            }
        }

        /// <summary>Stops a running fade and keeps the current opacity. The callback is not called.</summary>
        public void Cancel()
        {
            if (!IsFading)
            {
                return;
            }

            float current = _element.resolvedStyle.opacity;
            Reset();
            ClearTransition();
            _element.style.opacity = current;
        }

        private void Begin(float target, float duration, EaseType ease, bool hide, Action onComplete)
        {
            Reset();

            bool visible = _element.style.display != DisplayStyle.None && _element.resolvedStyle.display != DisplayStyle.None;
            bool instant = duration <= 0f || _element.panel == null || (target <= 0f && !visible);
            if (instant)
            {
                if (target > 0f)
                {
                    FadeInNow();
                }
                else
                {
                    FadeOutNow();
                }

                onComplete?.Invoke();
                return;
            }

            int version = _version;
            IsFading = true;
            _onComplete = onComplete;
            _hideOnComplete = hide;
            _element.RegisterCallback<TransitionEndEvent>(OnTransitionEnd);
            _listening = true;

            if (target > 0f && !visible)
            {
                ClearTransition();
                _element.style.opacity = 0f;
                _element.style.display = DisplayStyle.Flex;
                _showTimer = _element.schedule.Execute(() =>
                {
                    if (version != _version)
                    {
                        return;
                    }

                    ApplyTransition(duration, ease);
                    _element.style.opacity = target;
                }).StartingIn(ShowDelayMs);
                _fallbackTimer = _element.schedule.Execute(() => Complete(version)).StartingIn(ShowDelayMs + (long)(duration * 1000f) + FallbackMarginMs);
                return;
            }

            _element.style.display = DisplayStyle.Flex;
            ApplyTransition(duration, ease);
            _element.style.opacity = target;
            _fallbackTimer = _element.schedule.Execute(() => Complete(version)).StartingIn((long)(duration * 1000f) + FallbackMarginMs);
        }

        private void OnTransitionEnd(TransitionEndEvent evt)
        {
            // TransitionEndEvent bubbles, so ignore transitions of children and of other properties.
            if (evt.target == _element && AffectsOpacity(evt))
            {
                Complete(_version);
            }
        }

        private static bool AffectsOpacity(TransitionEndEvent evt)
        {
            foreach (StylePropertyName property in evt.stylePropertyNames)
            {
                if (property == Opacity)
                {
                    return true;
                }
            }

            return false;
        }

        private void Complete(int version)
        {
            if (version != _version || !IsFading)
            {
                return;
            }

            Action callback = _onComplete;
            bool hide = _hideOnComplete;
            Reset();
            ClearTransition();
            if (hide)
            {
                _element.style.display = DisplayStyle.None;
            }

            callback?.Invoke();
        }

        // Invalidates the running fade without touching the element's style.
        private void Reset()
        {
            _version++;
            IsFading = false;
            _onComplete = null;
            _hideOnComplete = false;
            _showTimer?.Pause();
            _fallbackTimer?.Pause();
            _showTimer = null;
            _fallbackTimer = null;
            if (_listening)
            {
                _element.UnregisterCallback<TransitionEndEvent>(OnTransitionEnd);
                _listening = false;
            }
        }

        private void ApplyTransition(float duration, EaseType ease)
        {
            IStyle style = _element.style;
            style.transitionProperty = new List<StylePropertyName> { Opacity };
            style.transitionDuration = new List<TimeValue> { new(duration, TimeUnit.Second) };
            style.transitionDelay = new List<TimeValue> { new(0f, TimeUnit.Second) };
            style.transitionTimingFunction = new List<EasingFunction> { new(ease.ToEasingMode()) };
        }

        private void ClearTransition()
        {
            IStyle style = _element.style;
            style.transitionProperty = StyleKeyword.Null;
            style.transitionDuration = StyleKeyword.Null;
            style.transitionDelay = StyleKeyword.Null;
            style.transitionTimingFunction = StyleKeyword.Null;
        }
    }
}
