using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>
    /// A serializable class from the scope's Inspector, and a ScriptableObject from the scope's Settings list
    /// (registered by PretzelLifetimeScope). Keep runtime state out of both.
    /// </summary>
    public sealed class SettingsInstaller : IInstaller
    {
        private readonly DifficultySettings _difficulty;
        private readonly bool _hasGameplayAsset;

        public SettingsInstaller(DifficultySettings difficulty, bool hasGameplayAsset)
        {
            _difficulty = difficulty;
            _hasGameplayAsset = hasGameplayAsset;
        }

        public void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_difficulty);

            if (!_hasGameplayAsset)
            {
                GameplaySettings defaults = ScriptableObject.CreateInstance<GameplaySettings>();
                defaults.name = "Default Gameplay Settings";
                builder.RegisterInstance(defaults);

                // The container never destroys Unity objects; a dispose callback does.
                builder.RegisterDisposeCallback(_ => Object.Destroy(defaults));
            }

            builder.RegisterEntryPoint<SettingsDemo>();
        }
    }
}
