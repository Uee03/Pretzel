using System;
using System.Runtime.CompilerServices;
using Pretzel.Core;
using Pretzel.Tween;
using UnityEngine.UIElements;

namespace Pretzel.UI
{
    public static class VisualElementFadeExtensions
    {
        private static readonly ConditionalWeakTable<VisualElement, UIToolkitFader> _faders = new();

        /// <summary>Returns the one fader of this element, creating it on first use.</summary>
        public static UIToolkitFader GetFader(this VisualElement element)
        {
            Guard.NotNull(element, nameof(element));
            return _faders.GetValue(element, e => new UIToolkitFader(e));
        }

        public static void Fade(this VisualElement element, FadeType type, float duration = UIToolkitFader.DefaultDuration, EaseType ease = EaseType.Linear, Action onComplete = null)
        {
            element.GetFader().Fade(type, duration, ease, onComplete);
        }

        public static void FadeIn(this VisualElement element, float duration = UIToolkitFader.DefaultDuration, EaseType ease = EaseType.Linear, Action onComplete = null)
        {
            element.GetFader().FadeIn(duration, ease, onComplete);
        }

        public static void FadeOut(this VisualElement element, float duration = UIToolkitFader.DefaultDuration, EaseType ease = EaseType.Linear, Action onComplete = null)
        {
            element.GetFader().FadeOut(duration, ease, onComplete);
        }

        public static void FadeInNow(this VisualElement element)
        {
            element.GetFader().FadeInNow();
        }

        public static void FadeOutNow(this VisualElement element)
        {
            element.GetFader().FadeOutNow();
        }
    }
}
