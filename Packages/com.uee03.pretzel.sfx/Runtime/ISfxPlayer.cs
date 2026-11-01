using Pretzel.Audio;
using UnityEngine;

namespace Pretzel.Sfx
{
    /// <summary>What game code needs to play a sound by name. Depend on this so tests can pass a fake.</summary>
    public interface ISfxPlayer
    {
        /// <summary>Plays a sound as 2D, or as its cue sets it. Returns <see cref="AudioHandle.None"/> when it did not play.</summary>
        AudioHandle Play(string id);

        /// <summary>Plays a sound at a world position.</summary>
        AudioHandle Play(string id, Vector3 position);

        /// <summary>Stops every voice of one sound.</summary>
        void Stop(string id);
    }
}
