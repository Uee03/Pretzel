using System;

namespace Pretzel.DI
{
    /// <summary>Thrown for a missing, duplicate or circular registration, or a type the container cannot construct.</summary>
    public sealed class ContainerException : Exception
    {
        public ContainerException(string message) : base(message)
        {
        }

        public ContainerException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
