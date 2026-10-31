using System;
using UnityEngine;

namespace Pretzel.Multiplayer
{
    /// <summary>A network prefab that is spawned from a pool.</summary>
    [Serializable]
    public sealed class PooledPrefabEntry
    {
        [Tooltip("A prefab with a NetworkObject. It must also be in the NetworkManager's network prefab list.")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("Idle instances created up front. Prewarming runs once when the bootstrap starts.")]
        [SerializeField, Min(0)] private int _prewarm = 8;

        [Tooltip("Idle instances kept beyond this are destroyed when released.")]
        [SerializeField, Min(1)] private int _maxIdle = 64;

        public GameObject Prefab => _prefab;

        public int Prewarm => _prewarm;

        public int MaxIdle => _maxIdle;
    }
}
