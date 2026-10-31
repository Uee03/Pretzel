using UnityEngine;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>
    /// Authored configuration. Create one with Assets > Create > Pretzel > Samples > Gameplay Settings and add it to the
    /// scope's Settings list; without one, the sample registers a default instance.
    /// </summary>
    [CreateAssetMenu(menuName = "Pretzel/Samples/Gameplay Settings", fileName = "GameplaySettings")]
    public sealed class GameplaySettings : ScriptableObject
    {
        [SerializeField, Min(0.1f)] private float _gameSpeed = 1f;
        [SerializeField] private string _startLevel = "Level 1";

        public float GameSpeed => _gameSpeed;

        public string StartLevel => _startLevel;
    }
}
