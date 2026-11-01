using System;
using Pretzel.Pooling;
using UnityEngine;

namespace SpaceInvaders.Gameplay
{
    /// <summary>One pooled invader. It animates between two frames on every march step and reports its own death.</summary>
    public sealed class Invader : MonoBehaviour, IDamageable, IPoolable
    {
        [SerializeField] private InvaderKind _kind;
        [SerializeField] private GameObject[] _frames = new GameObject[0];

        private int _frame;
        private bool _alive;

        public InvaderKind Kind => _kind;

        /// <summary>Column in the formation. The lowest invader of a column is the one that can drop a bomb.</summary>
        public int Column { get; set; }

        public event Action<Invader> Killed;

        public void OnTakenFromPool()
        {
            _alive = true;
            SetFrame(0);
        }

        public void OnReturnedToPool()
        {
            _alive = false;
            Killed = null;
        }

        public bool TakeHit(Team attacker)
        {
            if (!_alive || attacker != Team.Player)
            {
                return false;
            }

            _alive = false;
            Killed?.Invoke(this);
            return true;
        }

        public void AdvanceFrame()
        {
            SetFrame(_frame + 1);
        }

        private void SetFrame(int frame)
        {
            if (_frames.Length == 0)
            {
                return;
            }

            _frame = frame % _frames.Length;
            for (int i = 0; i < _frames.Length; i++)
            {
                _frames[i].SetActive(i == _frame);
            }
        }
    }
}
