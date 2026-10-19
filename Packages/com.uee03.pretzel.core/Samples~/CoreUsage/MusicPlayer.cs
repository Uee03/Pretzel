using Pretzel.Core.Singletons;
using UnityEngine;

namespace Pretzel.Samples.Core
{
    /// <summary>
    /// Regulator singleton: persists across scenes, and the newest instance replaces older ones.
    /// Put one in every scene that needs its own configuration, and the latest scene's copy wins.
    /// </summary>
    public class MusicPlayer : RegulatorSingleton<MusicPlayer>
    {
        [SerializeField] private AudioClip _clip;

        protected override void Awake()
        {
            base.Awake();
            Debug.Log($"MusicPlayer created at t={InitializationTime:0.00}s with clip '{(_clip ? _clip.name : "none")}'.", this);
        }
    }
}
