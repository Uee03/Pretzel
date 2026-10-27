using System;
using UnityEngine.Scripting;

namespace Pretzel.DI
{
    /// <summary>
    /// Marks the constructor to use, or a field, property or method to fill after construction.
    /// Derives from <see cref="PreserveAttribute"/>, so IL2CPP code stripping keeps the member.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class InjectAttribute : PreserveAttribute
    {
    }
}
