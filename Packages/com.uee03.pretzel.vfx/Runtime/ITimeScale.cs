namespace Pretzel.Vfx
{
    /// <summary>What game code needs to freeze or slow time for a moment. Depend on this so tests can pass a fake.</summary>
    public interface ITimeScale
    {
        /// <summary>Stops time for a few unscaled seconds. A hit stop is 0.04 to 0.1.</summary>
        int Freeze(float seconds);

        /// <summary>Slows time to <paramref name="scale"/> for a few unscaled seconds. Returns an id for <see cref="Cancel"/>.</summary>
        int SlowMotion(float scale, float seconds);

        bool Cancel(int id);
    }
}
