namespace Pretzel.Pooling
{
    /// <summary>Implement on any component of a pooled prefab to reset state between uses.</summary>
    public interface IPoolable
    {
        /// <summary>Called after the object is positioned and activated.</summary>
        void OnTakenFromPool();

        /// <summary>Called before the object is deactivated. Stop coroutines, tweens and subscriptions here.</summary>
        void OnReturnedToPool();
    }
}
