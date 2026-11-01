using VContainer;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>An open generic registration: IRepository&lt;PlayerRecord&gt; and IRepository&lt;ItemRecord&gt; from one line.</summary>
    public sealed class OpenGenericsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register(typeof(Repository<>), Lifetime.Singleton).As(typeof(IRepository<>));
            builder.RegisterEntryPoint<RepositoryDemo>();
        }
    }
}
