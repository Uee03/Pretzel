using UnityEngine;

namespace Pretzel.Samples.NoContainer
{
    /// <summary>
    /// Initializes <see cref="GameServices"/> once per Play session, before any scene's Awake and from any start scene.
    /// </summary>
    public static class GameServicesBootstrap
    {
        private static GameServices _services;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Start()
        {
            _services = Resources.Load<GameServices>(GameServices.ResourceName);
            if (_services == null)
            {
                Debug.LogError($"No GameServices asset at Resources/{GameServices.ResourceName}. Create one with Assets > Create > Pretzel > Samples > No Container > Game Services.");
                return;
            }

            _services.Initialize();

            // With domain reload off this class keeps its subscriptions between sessions, so subscribe exactly once.
            Application.quitting -= Stop;
            Application.quitting += Stop;
        }

        private static void Stop()
        {
            Application.quitting -= Stop;
            if (_services != null)
            {
                _services.Shutdown();
            }
        }
    }
}
