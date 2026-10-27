using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Pretzel.Core;
using UnityEngine;

namespace Pretzel.DI
{
    /// <summary>
    /// A built container or child scope. Thread-safe, though Unity objects should still be resolved on the main thread.
    /// Disposing it disposes its child scopes and every <see cref="IDisposable"/> it constructed, newest first.
    /// </summary>
    public sealed class Container : IObjectResolver
    {
        private readonly Container _parent;
        private readonly object _sync;
        private readonly HashSet<Registration> _resolving;
        private readonly Dictionary<Type, Registration> _registrations;
        private readonly List<Registration> _entryPoints;
        private readonly Dictionary<Registration, object> _instances = new();
        private readonly List<IDisposable> _disposables = new();
        private readonly List<Container> _children = new();
        private bool _disposed;

        public Container Parent => _parent;

        public bool IsDisposed => _disposed;

        internal Container(Container parent, Dictionary<Type, Registration> registrations, List<Registration> entryPoints)
        {
            _parent = parent;
            _sync = parent?._sync ?? new object();
            _resolving = parent?._resolving ?? new HashSet<Registration>();
            _registrations = registrations;
            _entryPoints = entryPoints;
        }

        public object Resolve(Type serviceType)
        {
            Guard.NotNull(serviceType, nameof(serviceType));
            if (TryResolve(serviceType, out object instance))
            {
                return instance;
            }

            throw new ContainerException($"{serviceType.Name} is not registered.");
        }

        public bool TryResolve(Type serviceType, out object instance)
        {
            Guard.NotNull(serviceType, nameof(serviceType));
            lock (_sync)
            {
                ThrowIfDisposed();
                if (serviceType == typeof(IObjectResolver) || serviceType == typeof(Container))
                {
                    instance = this;
                    return true;
                }

                Registration registration = Find(serviceType);
                if (registration == null)
                {
                    instance = null;
                    return false;
                }

                instance = Resolve(registration);
                return true;
            }
        }

        public bool IsRegistered(Type serviceType)
        {
            return serviceType == typeof(IObjectResolver) || serviceType == typeof(Container) || Find(serviceType) != null;
        }

        public void Inject(object target)
        {
            Guard.NotNull(target, nameof(target));
            InjectionInfo info = InjectionInfo.Get(target.GetType());
            if (!info.HasMembers)
            {
                return;
            }

            lock (_sync)
            {
                ThrowIfDisposed();
                InjectMembers(target, info);
            }
        }

        IObjectResolver IObjectResolver.CreateScope(Action<ContainerBuilder> configure) => CreateScope(configure);

        public Container CreateScope(Action<ContainerBuilder> configure = null)
        {
            ContainerBuilder builder = new();
            configure?.Invoke(builder);
            lock (_sync)
            {
                ThrowIfDisposed();
                Container child = builder.Build(this);
                _children.Add(child);
                return child;
            }
        }

        /// <summary>Creates the entry points registered in this scope, in registration order.</summary>
        public IReadOnlyList<object> ResolveEntryPoints()
        {
            List<object> result = new(_entryPoints.Count);
            lock (_sync)
            {
                ThrowIfDisposed();
                foreach (Registration registration in _entryPoints)
                {
                    result.Add(Resolve(registration));
                }
            }

            return result;
        }

        public void Dispose()
        {
            Container[] children;
            IDisposable[] disposables;
            lock (_sync)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                children = _children.ToArray();
                _children.Clear();
                disposables = _disposables.ToArray();
                _disposables.Clear();
                _instances.Clear();
                _parent?._children.Remove(this);
            }

            for (int i = children.Length - 1; i >= 0; i--)
            {
                children[i].Dispose();
            }

            for (int i = disposables.Length - 1; i >= 0; i--)
            {
                try
                {
                    disposables[i].Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        private Registration Find(Type serviceType)
        {
            for (Container scope = this; scope != null; scope = scope._parent)
            {
                if (scope._registrations.TryGetValue(serviceType, out Registration registration))
                {
                    return registration;
                }
            }

            return null;
        }

        private object Resolve(Registration registration)
        {
            if (registration.Instance != null)
            {
                return registration.Instance;
            }

            switch (registration.Lifetime)
            {
                case Lifetime.Singleton:
                    return registration.Owner.GetOrCreate(registration);
                case Lifetime.Scoped:
                    return GetOrCreate(registration);
                default:
                    return Create(registration);
            }
        }

        private object GetOrCreate(Registration registration)
        {
            if (_instances.TryGetValue(registration, out object existing))
            {
                return existing;
            }

            object created = Create(registration);
            _instances.Add(registration, created);
            return created;
        }

        private object Create(Registration registration)
        {
            ThrowIfDisposed();
            if (!_resolving.Add(registration))
            {
                throw new ContainerException($"Circular dependency while resolving {registration.ImplementationType.Name}.");
            }

            try
            {
                object instance = registration.Factory != null ? registration.Factory(this) : Construct(registration.ImplementationType);
                if (registration.Factory == null && instance is IDisposable disposable)
                {
                    _disposables.Add(disposable);
                }

                return instance;
            }
            finally
            {
                _resolving.Remove(registration);
            }
        }

        private object Construct(Type type)
        {
            InjectionInfo info = InjectionInfo.Get(type);
            if (info.Constructor == null)
            {
                throw new ContainerException($"{type.Name} has no public constructor.");
            }

            ParameterInfo[] parameters = info.ConstructorParameters;
            object[] arguments = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                arguments[i] = ResolveParameter(parameters[i], type);
            }

            object instance;
            try
            {
                instance = info.Constructor.Invoke(arguments);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }

            InjectMembers(instance, info);
            return instance;
        }

        private void InjectMembers(object target, InjectionInfo info)
        {
            Type type = target.GetType();
            foreach (FieldInfo field in info.Fields)
            {
                field.SetValue(target, ResolveMember(field.FieldType, type, field.Name));
            }

            foreach (PropertyInfo property in info.Properties)
            {
                property.SetValue(target, ResolveMember(property.PropertyType, type, property.Name));
            }

            foreach (MethodInfo method in info.Methods)
            {
                ParameterInfo[] parameters = method.GetParameters();
                object[] arguments = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    arguments[i] = ResolveParameter(parameters[i], type);
                }

                try
                {
                    method.Invoke(target, arguments);
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    throw;
                }
            }
        }

        private object ResolveParameter(ParameterInfo parameter, Type owner)
        {
            if (TryResolveUnlocked(parameter.ParameterType, out object value))
            {
                return value;
            }

            if (parameter.HasDefaultValue)
            {
                return parameter.DefaultValue;
            }

            throw new ContainerException($"{owner.Name} needs {parameter.ParameterType.Name} ('{parameter.Name}'), which is not registered.");
        }

        private object ResolveMember(Type memberType, Type owner, string memberName)
        {
            if (TryResolveUnlocked(memberType, out object value))
            {
                return value;
            }

            throw new ContainerException($"[Inject] {owner.Name}.{memberName} needs {memberType.Name}, which is not registered.");
        }

        private bool TryResolveUnlocked(Type serviceType, out object instance)
        {
            if (serviceType == typeof(IObjectResolver) || serviceType == typeof(Container))
            {
                instance = this;
                return true;
            }

            Registration registration = Find(serviceType);
            instance = registration == null ? null : Resolve(registration);
            return registration != null;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Container));
            }
        }
    }
}
