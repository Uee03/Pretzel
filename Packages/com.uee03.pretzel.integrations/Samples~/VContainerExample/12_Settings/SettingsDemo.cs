using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Settings are read-only dependencies, injected like services.</summary>
    public sealed class SettingsDemo : IStartable
    {
        private readonly ISampleLog _log;
        private readonly GameplaySettings _gameplay;
        private readonly DifficultySettings _difficulty;

        public SettingsDemo(ISampleLog log, GameplaySettings gameplay, DifficultySettings difficulty)
        {
            _log = log;
            _gameplay = gameplay;
            _difficulty = difficulty;
        }

        public void Start()
        {
            _log.Write("12 Settings", $"ScriptableObject '{_gameplay.name}': speed {_gameplay.GameSpeed}, start level {_gameplay.StartLevel}");
            _log.Write("12 Settings", $"Serializable class: enemy health {_difficulty.enemyHealth}, damage x{_difficulty.damageMultiplier}");
        }
    }
}
