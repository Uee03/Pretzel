using Pretzel.Tween;
using UnityEngine.UIElements;

namespace Pretzel.UI
{
    public static class UIEaseExtensions
    {
        /// <summary>
        /// Maps an <see cref="EaseType"/> to a USS <see cref="EasingMode"/>.
        /// UI Toolkit has no Quad, Quart, Quint or Expo curves. Quad uses the CSS ease-in/out curves,
        /// and Quart, Quint and Expo fall back to the nearest steeper match, Cubic.
        /// </summary>
        public static EasingMode ToEasingMode(this EaseType ease)
        {
            return ease switch
            {
                EaseType.Linear => EasingMode.Linear,
                EaseType.InSine => EasingMode.EaseInSine,
                EaseType.OutSine => EasingMode.EaseOutSine,
                EaseType.InOutSine => EasingMode.EaseInOutSine,
                EaseType.InQuad => EasingMode.EaseIn,
                EaseType.OutQuad => EasingMode.EaseOut,
                EaseType.InOutQuad => EasingMode.EaseInOut,
                EaseType.InCubic or EaseType.InQuart or EaseType.InQuint or EaseType.InExpo => EasingMode.EaseInCubic,
                EaseType.OutCubic or EaseType.OutQuart or EaseType.OutQuint or EaseType.OutExpo => EasingMode.EaseOutCubic,
                EaseType.InOutCubic or EaseType.InOutQuart or EaseType.InOutQuint or EaseType.InOutExpo => EasingMode.EaseInOutCubic,
                EaseType.InCirc => EasingMode.EaseInCirc,
                EaseType.OutCirc => EasingMode.EaseOutCirc,
                EaseType.InOutCirc => EasingMode.EaseInOutCirc,
                EaseType.InBack => EasingMode.EaseInBack,
                EaseType.OutBack => EasingMode.EaseOutBack,
                EaseType.InOutBack => EasingMode.EaseInOutBack,
                EaseType.InElastic => EasingMode.EaseInElastic,
                EaseType.OutElastic => EasingMode.EaseOutElastic,
                EaseType.InOutElastic => EasingMode.EaseInOutElastic,
                EaseType.InBounce => EasingMode.EaseInBounce,
                EaseType.OutBounce => EasingMode.EaseOutBounce,
                EaseType.InOutBounce => EasingMode.EaseInOutBounce,
                _ => EasingMode.Linear
            };
        }
    }
}
