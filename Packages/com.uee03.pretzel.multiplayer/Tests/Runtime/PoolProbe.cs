using Pretzel.Pooling;
using Unity.Netcode;

namespace Pretzel.Multiplayer.PlayModeTests
{
    /// <summary>Counts pool and network lifecycle callbacks so tests can see that an instance was recycled.</summary>
    public sealed class PoolProbe : NetworkBehaviour, IPoolable
    {
        public int Taken { get; private set; }

        public int Returned { get; private set; }

        public int Spawned { get; private set; }

        public int Despawned { get; private set; }

        public void OnTakenFromPool()
        {
            Taken++;
        }

        public void OnReturnedToPool()
        {
            Returned++;
        }

        public override void OnNetworkSpawn()
        {
            Spawned++;
        }

        public override void OnNetworkDespawn()
        {
            Despawned++;
        }
    }
}
