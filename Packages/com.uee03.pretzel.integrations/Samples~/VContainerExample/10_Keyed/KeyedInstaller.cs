using VContainer;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Two implementations of one interface told apart by a key. An enum key avoids string typos.</summary>
    public sealed class KeyedInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<IWeapon, Sword>(Lifetime.Singleton).Keyed(WeaponSlot.Melee);
            builder.Register<IWeapon, Bow>(Lifetime.Singleton).Keyed(WeaponSlot.Ranged);
            builder.RegisterEntryPoint<Armory>();
        }
    }
}
