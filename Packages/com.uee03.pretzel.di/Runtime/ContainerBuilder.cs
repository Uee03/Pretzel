using System;
using System.Collections.Generic;
using Pretzel.Core;

namespace Pretzel.DI
{
    /// <summary>Collects registrations and builds a <see cref="Container"/>. Build validates constructor dependencies up front.</summary>
    public sealed class ContainerBuilder
    {
        private readonly List<(Registration Registration, Type DefaultService)> _registrations = new();

        public RegistrationBuilder Register<T>(Lifetime lifetime) => Add(new Registration(typeof(T), lifetime), typeof(T));

        public RegistrationBuilder Register<TService, TImplementation>(Lifetime lifetime) where TImplementation : TService
        {
            return Add(new Registration(typeof(TImplementation), lifetime), typeof(TService));
        }

        public RegistrationBuilder Register(Type implementationType, Lifetime lifetime)
        {
            Guard.NotNull(implementationType, nameof(implementationType));
            return Add(new Registration(implementationType, lifetime), implementationType);
        }

        /// <summary>Registers an existing object. The container never disposes it; its creator owns it.</summary>
        public RegistrationBuilder RegisterInstance<T>(T instance) where T : class
        {
            Guard.NotNull(instance, nameof(instance));
            return Add(new Registration(instance.GetType(), Lifetime.Singleton, instance), typeof(T));
        }

        /// <summary>
        /// Registers a factory. Use it for Unity objects, configuration values and anything the container cannot construct.
        /// The container does not dispose what a factory returns.
        /// </summary>
        public RegistrationBuilder RegisterFactory<T>(Func<IObjectResolver, T> factory, Lifetime lifetime = Lifetime.Transient)
        {
            Guard.NotNull(factory, nameof(factory));
            return Add(new Registration(typeof(T), lifetime, factory: resolver => factory(resolver)), typeof(T));
        }

        /// <summary>
        /// Registers a type that is created when the scope starts and receives <see cref="IStartable"/>, <see cref="ITickable"/>,
        /// <see cref="ILateTickable"/> and <see cref="IFixedTickable"/> calls from its <see cref="LifetimeScope"/>.
        /// </summary>
        public RegistrationBuilder RegisterEntryPoint<T>(Lifetime lifetime = Lifetime.Singleton)
        {
            Registration registration = new(typeof(T), lifetime) { IsEntryPoint = true };
            return Add(registration, typeof(T));
        }

        public ContainerBuilder Install(IInstaller installer)
        {
            Guard.NotNull(installer, nameof(installer)).Install(this);
            return this;
        }

        public Container Build() => Build(null);

        internal Container Build(Container parent)
        {
            Dictionary<Type, Registration> map = new();
            List<Registration> entryPoints = new();
            foreach ((Registration registration, Type defaultService) in _registrations)
            {
                if (registration.ServiceTypes.Count == 0)
                {
                    registration.ServiceTypes.Add(defaultService);
                }

                foreach (Type service in registration.ServiceTypes)
                {
                    if (map.ContainsKey(service))
                    {
                        throw new ContainerException($"{service.Name} is registered more than once in the same scope.");
                    }

                    map.Add(service, registration);
                }

                if (registration.IsEntryPoint)
                {
                    entryPoints.Add(registration);
                }
            }

            foreach ((Registration registration, _) in _registrations)
            {
                Validate(registration, map, parent);
            }

            Container container = new(parent, map, entryPoints);
            foreach ((Registration registration, _) in _registrations)
            {
                registration.Owner = container;
            }

            return container;
        }

        private RegistrationBuilder Add(Registration registration, Type defaultService)
        {
            _registrations.Add((registration, defaultService));
            return new RegistrationBuilder(registration);
        }

        private static void Validate(Registration registration, Dictionary<Type, Registration> map, Container parent)
        {
            if (registration.Instance != null || registration.Factory != null)
            {
                return;
            }

            Type type = registration.ImplementationType;
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                throw new ContainerException($"{type.Name} is a Unity object, so the container cannot construct it. Use RegisterInstance or RegisterFactory.");
            }

            InjectionInfo info = InjectionInfo.Get(type);
            if (info.Constructor == null)
            {
                throw new ContainerException($"{type.Name} has no public constructor. Use RegisterInstance or RegisterFactory.");
            }

            foreach (System.Reflection.ParameterInfo parameter in info.ConstructorParameters)
            {
                Type needed = parameter.ParameterType;
                bool known = needed == typeof(IObjectResolver) || map.ContainsKey(needed) || (parent != null && parent.IsRegistered(needed));
                if (!known && !parameter.HasDefaultValue)
                {
                    throw new ContainerException($"{type.Name} needs {needed.Name} ('{parameter.Name}'), which is not registered.");
                }
            }
        }
    }
}
