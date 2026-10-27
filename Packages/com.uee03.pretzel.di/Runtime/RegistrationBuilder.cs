using System;

namespace Pretzel.DI
{
    /// <summary>
    /// Chooses the service types a registration answers to. Without a call, the registration answers to its own type,
    /// or to the service type given to <see cref="ContainerBuilder.Register{TService,TImplementation}"/>.
    /// </summary>
    public sealed class RegistrationBuilder
    {
        private readonly Registration _registration;

        internal RegistrationBuilder(Registration registration)
        {
            _registration = registration;
        }

        public RegistrationBuilder As<TService>() => As(typeof(TService));

        public RegistrationBuilder As(Type serviceType)
        {
            if (!serviceType.IsAssignableFrom(_registration.ImplementationType))
            {
                throw new ContainerException($"{_registration.ImplementationType.Name} does not implement {serviceType.Name}.");
            }

            if (!_registration.ServiceTypes.Contains(serviceType))
            {
                _registration.ServiceTypes.Add(serviceType);
            }

            return this;
        }

        public RegistrationBuilder AsSelf() => As(_registration.ImplementationType);

        /// <summary>Answers to every interface of the implementation, except <see cref="IDisposable"/>.</summary>
        public RegistrationBuilder AsImplementedInterfaces()
        {
            foreach (Type contract in _registration.ImplementationType.GetInterfaces())
            {
                if (contract != typeof(IDisposable))
                {
                    As(contract);
                }
            }

            return this;
        }
    }
}
