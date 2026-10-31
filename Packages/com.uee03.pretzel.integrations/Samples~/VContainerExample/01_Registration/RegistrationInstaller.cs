using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>The ways to tell the container what exists.</summary>
    public sealed class RegistrationInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            // Concrete type, with a constructor value that is not a service.
            builder.Register<Greeter>(Lifetime.Singleton).WithParameter("greeting", "Hello");

            // One instance behind two interfaces. AsImplementedInterfaces would register both as well.
            builder.Register<Score>(Lifetime.Singleton).As<IScoreReader, IScoreWriter>();

            // Interface to implementation, built by a delegate.
            builder.Register<IDiceRoller>(_ => new DiceRoller(seed: 7), Lifetime.Singleton);

            // An object that already exists. The container will not dispose it.
            builder.RegisterInstance(new BuildInfo(Application.version));

            builder.RegisterEntryPoint<RegistrationDemo>();
        }
    }
}
