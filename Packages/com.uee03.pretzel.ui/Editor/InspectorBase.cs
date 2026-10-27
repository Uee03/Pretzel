using UnityEngine.UIElements;

namespace Pretzel.UI.Editor
{
    /// <summary>
    /// Base class for a UI Toolkit custom inspector. Override <see cref="BuildInspector"/>; call <see cref="AddDefaultProperties"/>
    /// for the fields you do not customize. The root gets the editor skin class and an optional stylesheet.
    /// </summary>
    public abstract class InspectorBase : UnityEditor.Editor
    {
        /// <summary>Asset path of a stylesheet added to the root, or null.</summary>
        protected virtual string StyleSheetPath => null;

        public sealed override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();
            root.AddToClassList(EditorUI.Block);
            EditorUI.ApplySkinClass(root);
            if (!string.IsNullOrEmpty(StyleSheetPath))
            {
                StyleSheet sheet = EditorUI.LoadStyleSheet(StyleSheetPath);
                if (sheet)
                {
                    root.styleSheets.Add(sheet);
                }
            }

            BuildInspector(root);
            return root;
        }

        protected abstract void BuildInspector(VisualElement root);

        /// <summary>Adds bound fields for every visible property except the excluded property paths.</summary>
        protected VisualElement AddDefaultProperties(VisualElement parent, params string[] exclude)
        {
            VisualElement fields = EditorUI.PropertyFields(serializedObject, true, exclude);
            parent.Add(fields);
            return fields;
        }
    }
}
