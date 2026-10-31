using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Pretzel.Multiplayer.Samples.TwoCubes
{
    /// <summary>
    /// Place it in the scene on a GameObject with a NetworkObject. Any client can ask for an item or for all items to be deleted; the server decides and acts.
    /// Items spawn through the pooled prefab handler registered by PretzelNetworkBootstrap, so Delete returns them to the pool instead of destroying them.
    /// </summary>
    public sealed class TwoCubesItemSpawner : NetworkBehaviour
    {
        private readonly NetworkVariable<int> _itemCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly List<NetworkObject> _items = new List<NetworkObject>();

        [SerializeField] private GameObject _itemPrefab;
        [SerializeField, Min(1)] private int _maxItems = 30;
        [SerializeField, Min(0.5f)] private float _arenaRadius = 7f;
        [SerializeField] private float _spawnHeight = 0.5f;

        public int ItemCount => _itemCount.Value;

        /// <summary>Raised on every instance when the replicated item count changes, and once after spawn.</summary>
        public event Action<int> ItemCountChanged;

        public override void OnNetworkSpawn()
        {
            _itemCount.OnValueChanged += OnCountChanged;
            ItemCountChanged?.Invoke(_itemCount.Value);
        }

        public override void OnNetworkDespawn()
        {
            _itemCount.OnValueChanged -= OnCountChanged;
            _items.Clear();
        }

        /// <summary>Asks the server to spawn one item. Works on host and clients.</summary>
        public void RequestSpawn()
        {
            if (IsSpawned)
            {
                SpawnItemRpc();
            }
        }

        /// <summary>Asks the server to delete every item. Works on host and clients.</summary>
        public void RequestDeleteAll()
        {
            if (IsSpawned)
            {
                DeleteItemsRpc();
            }
        }

        // Any client may ask. The server validates: it owns the item list, the cap and the position.
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SpawnItemRpc()
        {
            if (_itemPrefab == null || _items.Count >= _maxItems)
            {
                return;
            }

            Vector2 offset = UnityEngine.Random.insideUnitCircle * _arenaRadius;
            NetworkObject item = NetworkObject.InstantiateAndSpawn(_itemPrefab, NetworkManager, NetworkManager.ServerClientId, false, false, false,
                new Vector3(offset.x, _spawnHeight, offset.y), Quaternion.identity);
            if (item == null)
            {
                return;
            }

            _items.Add(item);
            _itemCount.Value = _items.Count;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void DeleteItemsRpc()
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                NetworkObject item = _items[i];
                if (item != null && item.IsSpawned)
                {
                    item.Despawn(true);
                }
            }

            _items.Clear();
            _itemCount.Value = 0;
        }

        private void OnCountChanged(int previous, int current)
        {
            ItemCountChanged?.Invoke(current);
        }
    }
}
