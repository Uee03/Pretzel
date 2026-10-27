using System;
using System.Collections.Generic;

namespace Pretzel.DI
{
    /// <summary>How one implementation is created and which service types it answers to.</summary>
    internal sealed class Registration
    {
        public Type ImplementationType { get; }
        public Lifetime Lifetime { get; }
        public object Instance { get; }
        public Func<IObjectResolver, object> Factory { get; }
        public List<Type> ServiceTypes { get; } = new();
        public bool IsEntryPoint { get; set; }

        /// <summary>The container that built this registration. Singletons live there.</summary>
        public Container Owner { get; set; }

        public Registration(Type implementationType, Lifetime lifetime, object instance = null, Func<IObjectResolver, object> factory = null)
        {
            ImplementationType = implementationType;
            Lifetime = lifetime;
            Instance = instance;
            Factory = factory;
        }
    }
}
