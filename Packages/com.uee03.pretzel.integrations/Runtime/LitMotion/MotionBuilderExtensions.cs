using LitMotion;
using Pretzel.Tween;

namespace Pretzel.Integrations.LitMotion
{
    public static class MotionBuilderExtensions
    {
        /// <summary>Sets the ease of a LitMotion builder from a Pretzel <see cref="EaseType"/>.</summary>
        public static MotionBuilder<TValue, TOptions, TAdapter> WithEase<TValue, TOptions, TAdapter>(
            this MotionBuilder<TValue, TOptions, TAdapter> builder, EaseType ease)
            where TValue : unmanaged
            where TOptions : unmanaged, IMotionOptions
            where TAdapter : unmanaged, IMotionAdapter<TValue, TOptions>
        {
            return builder.WithEase(ease.ToLitMotionEase());
        }
    }
}
