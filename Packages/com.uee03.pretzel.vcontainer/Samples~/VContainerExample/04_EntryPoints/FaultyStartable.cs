using System;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Throws on purpose. The exception handler registered in <see cref="EntryPointsInstaller"/> receives it.</summary>
    public sealed class FaultyStartable : IStartable
    {
        public void Start() => throw new InvalidOperationException("FaultyStartable failed on purpose.");
    }
}
