using Pretzel.Integrations.VContainer;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>The only LifetimeScope of the sample. Every folder adds its registrations through its own installer.</summary>
    [RequireComponent(typeof(UIDocument), typeof(SceneInstaller), typeof(SceneMarker))]
    public sealed class VContainerSamplesScope : PretzelLifetimeScope
    {
        [SerializeField] private DifficultySettings _difficulty = new();
        [SerializeField] private BulletSettings _bullets = new();

        protected override void Configure(IContainerBuilder builder)
        {
            // Settings assets, ScriptableInstaller assets and MonoInstallers on this GameObject (13_Installers).
            base.Configure(builder);

            new SharedInstaller(GetComponent<UIDocument>()).Install(builder);
            new RegistrationInstaller().Install(builder);
            new LifetimesInstaller().Install(builder);
            new InjectionInstaller().Install(builder);
            new EntryPointsInstaller().Install(builder);
            new TimersInstaller().Install(builder);
            new BulletsInstaller(_bullets).Install(builder);
            new FactoriesInstaller().Install(builder);
            new ComponentsInstaller().Install(builder);
            new CollectionsInstaller().Install(builder);
            new KeyedInstaller().Install(builder);
            new OpenGenericsInstaller().Install(builder);
            new SettingsInstaller(_difficulty, HasSettingsAsset()).Install(builder);
            new CallbacksInstaller().Install(builder);
            new ChildScopesInstaller().Install(builder);
        }

        private bool HasSettingsAsset()
        {
            foreach (ScriptableObject asset in Settings)
            {
                if (asset is GameplaySettings)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
