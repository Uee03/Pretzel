using System;
using Unity.Netcode;

namespace SpaceInvaders.Coop
{
    /// <summary>The numbers every machine shows for one player.</summary>
    // Value type: an element of a replicated NetworkList; value equality lets NGO tell which entries changed.
    public struct CoopPlayerStat : INetworkSerializable, IEquatable<CoopPlayerStat>
    {
        public int Slot;
        public int Lives;
        public int Score;

        public bool IsEliminated => Lives <= 0;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Slot);
            serializer.SerializeValue(ref Lives);
            serializer.SerializeValue(ref Score);
        }

        public bool Equals(CoopPlayerStat other)
        {
            return Slot == other.Slot && Lives == other.Lives && Score == other.Score;
        }

        public override bool Equals(object obj)
        {
            return obj is CoopPlayerStat other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Slot, Lives, Score);
        }
    }
}
