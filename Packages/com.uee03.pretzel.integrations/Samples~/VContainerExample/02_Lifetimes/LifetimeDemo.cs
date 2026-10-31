using VContainer;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    public sealed class LifetimeDemo : IStartable
    {
        private const string Topic = "02 Lifetimes";

        private readonly ISampleLog _log;
        private readonly SingletonExample _singletonA;
        private readonly SingletonExample _singletonB;
        private readonly TransientExample _transientA;
        private readonly TransientExample _transientB;
        private readonly ScopedExample _scoped;
        private readonly IObjectResolver _resolver;

        // Taking the resolver is only right here because creating a scope is the point of the demo.
        public LifetimeDemo(
            ISampleLog log,
            SingletonExample singletonA,
            SingletonExample singletonB,
            TransientExample transientA,
            TransientExample transientB,
            ScopedExample scoped,
            IObjectResolver resolver)
        {
            _log = log;
            _singletonA = singletonA;
            _singletonB = singletonB;
            _transientA = transientA;
            _transientB = transientB;
            _scoped = scoped;
            _resolver = resolver;
        }

        public void Start()
        {
            _log.Write(Topic, $"Singleton: #{_singletonA.Id} and #{_singletonB.Id} (shared)");
            _log.Write(Topic, $"Transient: #{_transientA.Id} and #{_transientB.Id} (new on every resolve)");

            using (IScopedObjectResolver scope = _resolver.CreateScope())
            {
                ScopedExample inner = scope.Resolve<ScopedExample>();
                SingletonExample singleton = scope.Resolve<SingletonExample>();
                _log.Write(Topic, $"Scoped: #{_scoped.Id} in the scope, #{inner.Id} in a child scope; the child still shares singleton #{singleton.Id}");
            }
        }
    }
}
