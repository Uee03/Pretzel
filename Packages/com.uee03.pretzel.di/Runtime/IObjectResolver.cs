using System;

namespace Pretzel.DI
{
    /// <summary>Resolves registered services. Dispose it to dispose every instance it created and its child scopes.</summary>
    public interface IObjectResolver : IDisposable
    {
        object Resolve(Type serviceType);

        bool TryResolve(Type serviceType, out object instance);

        bool IsRegistered(Type serviceType);

        /// <summary>Fills the [Inject] fields, properties and methods of an existing object, such as a MonoBehaviour.</summary>
        void Inject(object target);

        /// <summary>Creates a child scope. Its registrations are added on top of this container's.</summary>
        IObjectResolver CreateScope(Action<ContainerBuilder> configure = null);
    }
}
