using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Pretzel.DI
{
    /// <summary>Cached constructor and [Inject] members of one type.</summary>
    internal sealed class InjectionInfo
    {
        private const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private static readonly ConcurrentDictionary<Type, InjectionInfo> Cache = new();

        public ConstructorInfo Constructor { get; }
        public ParameterInfo[] ConstructorParameters { get; }
        public FieldInfo[] Fields { get; }
        public PropertyInfo[] Properties { get; }
        public MethodInfo[] Methods { get; }

        public bool HasMembers => Fields.Length > 0 || Properties.Length > 0 || Methods.Length > 0;

        private InjectionInfo(Type type)
        {
            Constructor = FindConstructor(type);
            ConstructorParameters = Constructor?.GetParameters() ?? Array.Empty<ParameterInfo>();

            List<FieldInfo> fields = new();
            List<PropertyInfo> properties = new();
            List<MethodInfo> methods = new();
            for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                foreach (FieldInfo field in current.GetFields(MemberFlags))
                {
                    if (field.IsDefined(typeof(InjectAttribute), true))
                    {
                        fields.Add(field);
                    }
                }

                foreach (PropertyInfo property in current.GetProperties(MemberFlags))
                {
                    if (property.IsDefined(typeof(InjectAttribute), true))
                    {
                        if (!property.CanWrite)
                        {
                            throw new ContainerException($"[Inject] property {type.Name}.{property.Name} has no setter.");
                        }

                        properties.Add(property);
                    }
                }

                foreach (MethodInfo method in current.GetMethods(MemberFlags))
                {
                    if (method.IsDefined(typeof(InjectAttribute), true))
                    {
                        methods.Add(method);
                    }
                }
            }

            // Base class members first, so a derived [Inject] method can rely on them.
            fields.Reverse();
            properties.Reverse();
            methods.Reverse();
            Fields = fields.ToArray();
            Properties = properties.ToArray();
            Methods = methods.ToArray();
        }

        public static InjectionInfo Get(Type type) => Cache.GetOrAdd(type, t => new InjectionInfo(t));

        private static ConstructorInfo FindConstructor(Type type)
        {
            if (type.IsAbstract || type.IsInterface)
            {
                return null;
            }

            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            ConstructorInfo best = null;
            foreach (ConstructorInfo constructor in constructors)
            {
                if (constructor.IsDefined(typeof(InjectAttribute), true))
                {
                    return constructor;
                }

                if (best == null || constructor.GetParameters().Length > best.GetParameters().Length)
                {
                    best = constructor;
                }
            }

            return best;
        }
    }
}
