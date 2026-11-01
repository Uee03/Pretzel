using System;

namespace Pretzel.Core
{
    public static class DisposableExtensions
    {
        /// <summary>Hands a subscription to a bag: <c>bus.Subscribe&lt;Hit&gt;(OnHit).AddTo(_subscriptions);</c></summary>
        public static T AddTo<T>(this T item, CompositeDisposable bag) where T : IDisposable
        {
            Guard.NotNull(bag, nameof(bag));
            return bag.Add(item);
        }
    }
}
