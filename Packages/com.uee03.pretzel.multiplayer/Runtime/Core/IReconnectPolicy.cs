using System;

namespace Pretzel.Multiplayer
{
    /// <summary>Decides how long to wait before each automatic reconnect attempt, and when to stop.</summary>
    public interface IReconnectPolicy
    {
        /// <summary>The delay before the 1-based <paramref name="attempt"/>, or null to stop retrying.</summary>
        TimeSpan? GetDelay(int attempt);
    }
}
