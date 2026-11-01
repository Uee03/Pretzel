using Pretzel.Multiplayer;
using UnityEngine;

namespace SpaceInvaders.Coop
{
    /// <summary>
    /// Ties the NetworkManager to the Coop scene. NGO marks its NetworkManager DontDestroyOnLoad, so without this it would outlive the scene
    /// and clash with the new one the next time a player opens co-op from the title. Destroying it also shuts the session down.
    /// </summary>
    public sealed class CoopNetworkLifetime : MonoBehaviour
    {
        [SerializeField] private PretzelNetworkBootstrap _bootstrap;

        private void OnDestroy()
        {
            if (_bootstrap != null)
            {
                Destroy(_bootstrap.gameObject);
            }
        }
    }
}
