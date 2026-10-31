using UnityEngine;

namespace Pretzel.Multiplayer.Samples.TwoCubes
{
    public enum TwoCubesPlane
    {
        /// <summary>Top-down style: move on X and Z, face the movement direction, jump on Y.</summary>
        Horizontal3D = 0,

        /// <summary>Side view: move on X, turn around (yaw 0 or 180) when changing direction, jump on Y.</summary>
        Side2D = 1,
    }
}
