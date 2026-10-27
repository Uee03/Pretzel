using UnityEngine;

namespace Pretzel.Pooling.Tests
{
    public sealed class PoolableProbe : MonoBehaviour, IPoolable
    {
        public int Taken;
        public int Returned;
        public bool WasActiveWhenTaken;

        public void OnTakenFromPool()
        {
            Taken++;
            WasActiveWhenTaken = gameObject.activeSelf;
        }

        public void OnReturnedToPool() => Returned++;
    }
}
