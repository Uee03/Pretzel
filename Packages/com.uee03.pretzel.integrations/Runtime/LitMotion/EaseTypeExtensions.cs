using LitMotion;
using Pretzel.Tween;

namespace Pretzel.Integrations.LitMotion
{
    public static class EaseTypeExtensions
    {
        /// <summary>Maps a Pretzel <see cref="EaseType"/> to the LitMotion <see cref="Ease"/> with the same name.</summary>
        public static Ease ToLitMotionEase(this EaseType ease)
        {
            return ease switch
            {
                EaseType.Linear => Ease.Linear,
                EaseType.InSine => Ease.InSine,
                EaseType.OutSine => Ease.OutSine,
                EaseType.InOutSine => Ease.InOutSine,
                EaseType.InQuad => Ease.InQuad,
                EaseType.OutQuad => Ease.OutQuad,
                EaseType.InOutQuad => Ease.InOutQuad,
                EaseType.InCubic => Ease.InCubic,
                EaseType.OutCubic => Ease.OutCubic,
                EaseType.InOutCubic => Ease.InOutCubic,
                EaseType.InQuart => Ease.InQuart,
                EaseType.OutQuart => Ease.OutQuart,
                EaseType.InOutQuart => Ease.InOutQuart,
                EaseType.InQuint => Ease.InQuint,
                EaseType.OutQuint => Ease.OutQuint,
                EaseType.InOutQuint => Ease.InOutQuint,
                EaseType.InExpo => Ease.InExpo,
                EaseType.OutExpo => Ease.OutExpo,
                EaseType.InOutExpo => Ease.InOutExpo,
                EaseType.InCirc => Ease.InCirc,
                EaseType.OutCirc => Ease.OutCirc,
                EaseType.InOutCirc => Ease.InOutCirc,
                EaseType.InBack => Ease.InBack,
                EaseType.OutBack => Ease.OutBack,
                EaseType.InOutBack => Ease.InOutBack,
                EaseType.InElastic => Ease.InElastic,
                EaseType.OutElastic => Ease.OutElastic,
                EaseType.InOutElastic => Ease.InOutElastic,
                EaseType.InBounce => Ease.InBounce,
                EaseType.OutBounce => Ease.OutBounce,
                EaseType.InOutBounce => Ease.InOutBounce,
                _ => Ease.Linear
            };
        }
    }
}
