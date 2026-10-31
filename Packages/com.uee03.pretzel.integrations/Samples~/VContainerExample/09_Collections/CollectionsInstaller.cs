using VContainer;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Several implementations of one interface, resolved together as IReadOnlyList or IEnumerable.</summary>
    public sealed class CollectionsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<IReward, CoinReward>(Lifetime.Singleton);
            builder.Register<IReward, GemReward>(Lifetime.Singleton);
            builder.Register<IReward, ExperienceReward>(Lifetime.Singleton);
            builder.RegisterEntryPoint<RewardDemo>();
        }
    }
}
