namespace Pretzel.Audio
{
    /// <summary>Identifies one playing sound. Stale after the sound ends, so a handle never stops a reused AudioSource.</summary>
    public readonly struct AudioHandle
    {
        internal readonly int Id;
        internal readonly int Version;

        internal AudioHandle(int id, int version)
        {
            Id = id;
            Version = version;
        }

        public bool IsValid => Version != 0;

        public static AudioHandle None => default;
    }
}
