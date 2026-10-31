using UnityEditor;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI.PropertyDrawers.Editors
{
    /// <summary>Draws a [SerializeReference] field (or each element of a list) with a dropdown of the concrete types.</summary>
    [CustomPropertyDrawer(typeof(SerializeReferenceSelectorAttribute))]
    public sealed class SerializeReferenceSelectorDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                return new HelpBox("[SerializeReferenceSelector] needs a [SerializeReference] field.", HelpBoxMessageType.Error);
            }

            return new ReferenceSelectorElement(property);
        }
    }
}
