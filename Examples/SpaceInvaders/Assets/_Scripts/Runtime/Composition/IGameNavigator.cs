using System.Threading;
using UnityEngine;

namespace SpaceInvaders
{
    /// <summary>Moves between the title and the game. Both calls log their own failures, so callers can start them without awaiting.</summary>
    public interface IGameNavigator
    {
        Awaitable PlayAsync();

    /// <summary>Opens the co-op scene, where players choose local or online play.</summary>
    Awaitable PlayCoopAsync();

        Awaitable ReturnToTitleAsync();
    }
}
