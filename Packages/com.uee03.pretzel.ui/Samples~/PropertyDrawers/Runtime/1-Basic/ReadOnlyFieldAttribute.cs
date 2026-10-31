using UnityEngine;

namespace Pretzel.Samples.UI.PropertyDrawers
{
    /// <summary>Shows the field but does not let the Inspector change it.</summary>
    public sealed class ReadOnlyFieldAttribute : PropertyAttribute
    {
    }
}
