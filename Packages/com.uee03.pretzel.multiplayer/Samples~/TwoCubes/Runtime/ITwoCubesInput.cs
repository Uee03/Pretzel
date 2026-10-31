using UnityEngine;

namespace Pretzel.Multiplayer.Samples.TwoCubes
{
    /// <summary>Where a player's movement input comes from. Replace it to use gamepads or scripted input (the smoke test does).</summary>
    public interface ITwoCubesInput
    {
        /// <summary>Movement on two axes in [-1, 1]. X is left/right, Y is up/down (forward/back in 3D).</summary>
        Vector2 ReadMove(int localPlayerIndex);

        bool ReadJump(int localPlayerIndex);
    }
}
