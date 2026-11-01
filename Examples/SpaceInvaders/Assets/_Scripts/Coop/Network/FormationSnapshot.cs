using System;
using Unity.Netcode;
using UnityEngine;

namespace SpaceInvaders.Coop
{
    /// <summary>
    /// What a client needs to draw the host's invader formation: which wave, where the block is, which animation frame, and which
    /// invaders are alive (one bit per grid slot). <see cref="Generation"/> changes when a new wave starts, so a client rebuilds
    /// the formation instead of only moving it.
    /// </summary>
    // Value type: a replicated NetworkVariable payload; value equality lets NGO send it only when something changed.
    public struct FormationSnapshot : INetworkSerializable, IEquatable<FormationSnapshot>
    {
        public int Generation;
        public int Wave;
        public int DropRows;
        public Vector2 Origin;
        public int StepIndex;
        public ulong AliveLow;
        public ulong AliveHigh;

        public bool IsEmpty => Wave <= 0;

        /// <summary>One bit for each grid slot, in two 64-bit masks. A wave with more invaders than this cannot be replicated.</summary>
        public const int MaxInvaders = 128;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Generation);
            serializer.SerializeValue(ref Wave);
            serializer.SerializeValue(ref DropRows);
            serializer.SerializeValue(ref Origin);
            serializer.SerializeValue(ref StepIndex);
            serializer.SerializeValue(ref AliveLow);
            serializer.SerializeValue(ref AliveHigh);
        }

        public bool Equals(FormationSnapshot other)
        {
            return Generation == other.Generation && Wave == other.Wave && DropRows == other.DropRows && Origin == other.Origin
                   && StepIndex == other.StepIndex && AliveLow == other.AliveLow && AliveHigh == other.AliveHigh;
        }

        public override bool Equals(object obj)
        {
            return obj is FormationSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Generation, Wave, Origin, StepIndex, AliveLow, AliveHigh);
        }
    }
}
