using System;
using Unity.Netcode;
using UnityEngine;

namespace Pretzel.Multiplayer
{
    /// <summary>
    /// Put this on a player prefab. The host assigns the slot (unique per player object), the LocalPlayerIndex (0 for the first player on a device, 1 for the second ...)
    /// and the client that controls it. Gameplay code maps <see cref="LocalPlayerIndex"/> to an input scheme.
    /// </summary>
    /// <remarks>
    /// Player objects are ordinary owned NetworkObjects, not NGO "player objects", so the host can hand them to a returning client with ChangeOwnership.
    /// While nobody controls an object (its client dropped and the grace window runs) <see cref="IsLocallyControlled"/> is false everywhere, even on the host that inherits ownership.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class NetworkPlayerIdentity : NetworkBehaviour
    {
        public const ulong NoController = ulong.MaxValue;

        private readonly NetworkVariable<byte> _slotIndex = new NetworkVariable<byte>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<byte> _localPlayerIndex = new NetworkVariable<byte>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<ulong> _controllerClientId = new NetworkVariable<ulong>(NoController, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public int SlotIndex => _slotIndex.Value;

        public int LocalPlayerIndex => _localPlayerIndex.Value;

        public ulong ControllerClientId => _controllerClientId.Value;

        /// <summary>True on the device whose client controls this player. Use it, not IsOwner, to decide whether to read input.</summary>
        public bool IsLocallyControlled => IsSpawned && IsOwner && _controllerClientId.Value == NetworkManager.LocalClientId;

        /// <summary>Raised when the slot, local index or controller changes, and once after spawn.</summary>
        public event Action<NetworkPlayerIdentity> IdentityChanged;

        public override void OnNetworkSpawn()
        {
            _slotIndex.OnValueChanged += OnByteChanged;
            _localPlayerIndex.OnValueChanged += OnByteChanged;
            _controllerClientId.OnValueChanged += OnControllerChanged;
            IdentityChanged?.Invoke(this);
        }

        public override void OnNetworkDespawn()
        {
            _slotIndex.OnValueChanged -= OnByteChanged;
            _localPlayerIndex.OnValueChanged -= OnByteChanged;
            _controllerClientId.OnValueChanged -= OnControllerChanged;
        }

        /// <summary>Server only, after the object is spawned.</summary>
        public void ServerSetIdentity(int slotIndex, int localPlayerIndex, ulong controllerClientId)
        {
            if (!IsServer)
            {
                return;
            }

            _slotIndex.Value = (byte)Mathf.Clamp(slotIndex, 0, byte.MaxValue);
            _localPlayerIndex.Value = (byte)Mathf.Clamp(localPlayerIndex, 0, byte.MaxValue);
            _controllerClientId.Value = controllerClientId;
            IdentityChanged?.Invoke(this);
        }

        /// <summary>Server only. Use <see cref="NoController"/> while nobody controls the object.</summary>
        public void ServerSetController(ulong controllerClientId)
        {
            if (IsServer)
            {
                _controllerClientId.Value = controllerClientId;
                IdentityChanged?.Invoke(this);
            }
        }

        private void OnByteChanged(byte previous, byte current)
        {
            IdentityChanged?.Invoke(this);
        }

        private void OnControllerChanged(ulong previous, ulong current)
        {
            IdentityChanged?.Invoke(this);
        }
    }
}
