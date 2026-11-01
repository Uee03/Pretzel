using Pretzel.Tween;
using UnityEngine;

namespace SpaceInvaders.Gameplay
{
    public static class MarchTempo
    {
        /// <summary>Seconds between steps. The fewer invaders are left, the faster they march, accelerating towards the last one.</summary>
        public static float StepInterval(int alive, int total, float slowest, float fastest)
        {
            if (total <= 1 || alive <= 0)
            {
                return alive <= 0 ? slowest : fastest;
            }

            float remaining = Mathf.Clamp01((alive - 1f) / (total - 1f));
            return EaseUtility.Lerp(EaseType.InQuad, fastest, slowest, remaining);
        }
    }
}
