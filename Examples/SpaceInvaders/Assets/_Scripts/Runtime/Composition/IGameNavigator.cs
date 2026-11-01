using System.Threading;
using UnityEngine;

namespace SpaceInvaders
{
    /// <summary>Moves between the title and the game. Both calls log their own failures, so callers can start them without awaiting.</summary>
    public interface IGameNavigator
    {
        Awaitable PlayAsync();

        Awaitable ReturnToTitleAsync();
    }
}
