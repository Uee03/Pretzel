using UnityEngine;

namespace SpaceInvaders.Gameplay
{
    /// <summary>The row of bunkers. Restores every brick at the start of a wave.</summary>
    public sealed class BunkerField : MonoBehaviour
    {
        private BunkerBlock[] _blocks;

        private void Awake()
        {
            _blocks = GetComponentsInChildren<BunkerBlock>(true);
        }

        public void RestoreAll()
        {
            foreach (BunkerBlock block in _blocks)
            {
                block.Restore();
            }
        }
    }
}
