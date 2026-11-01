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

        public int BlockCount => _blocks.Length;

        /// <summary>Copies each brick's health into <paramref name="buffer"/>, in a hierarchy order that is the same on every machine.</summary>
        public void ReadHealths(byte[] buffer)
        {
            int count = Mathf.Min(buffer.Length, _blocks.Length);
            for (int i = 0; i < count; i++)
            {
                buffer[i] = (byte)_blocks[i].Health;
            }
        }

        public void SetHealth(int index, int health)
        {
            if (index >= 0 && index < _blocks.Length)
            {
                _blocks[index].SetHealth(health);
            }
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
