using System;
using Unity.Netcode;
using UnityEngine;

namespace SpaceInvaders.Coop
{
    /// <summary>Whether the mystery ship is flying and where.</summary>
    // Value type: a replicated NetworkVariable payload; value equality lets NGO send it only when something changed.
    public struct UfoSnapshot : INetworkSerializable, IEquatable<UfoSnapshot>
    {
        public bool Visible;
        public Vector2 Position;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Visible);
            serializer.SerializeValue(ref Position);
        }

        public bool Equals(UfoSnapshot other)
        {
            return Visible == other.Visible && Position == other.Position;
        }

        public override bool Equals(object obj)
        {
            return obj is UfoSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Visible, Position);
        }
    }
}
