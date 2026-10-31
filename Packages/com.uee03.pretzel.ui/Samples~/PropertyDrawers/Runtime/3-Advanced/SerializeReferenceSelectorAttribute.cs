using UnityEngine;

namespace Pretzel.Samples.UI.PropertyDrawers
{
    /// <summary>Put it on a [SerializeReference] field (or list) of an abstract type to pick the concrete type from a dropdown.</summary>
    public sealed class SerializeReferenceSelectorAttribute : PropertyAttribute
    {
    }
}
