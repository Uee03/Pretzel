using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>The log every demo writes to, and the view that shows it.</summary>
    public sealed class SharedInstaller : IInstaller
    {
        private readonly UIDocument _document;

        public SharedInstaller(UIDocument document) => _document = document;

        public void Install(IContainerBuilder builder)
        {
            // One instance resolvable as the concrete type (for the view) and as the interface (for the demos).
            builder.Register<SampleLog>(Lifetime.Singleton).AsSelf().As<ISampleLog>();

            // An existing component on the scope's GameObject.
            builder.RegisterComponent(_document);
            builder.RegisterEntryPoint<SampleLogView>();
        }
    }
}
