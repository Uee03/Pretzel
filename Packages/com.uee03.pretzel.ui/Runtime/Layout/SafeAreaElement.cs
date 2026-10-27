using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.UI
{
    /// <summary>
    /// A full-size container that pads its content to <see cref="Screen.safeArea"/>. Put it at the root of a runtime screen
    /// and place the content inside it. The padding updates when the panel resizes, for example on rotation.
    /// </summary>
    [UxmlElement]
    public partial class SafeAreaElement : VisualElement
    {
        public const string UssClassName = "pretzel-safe-area";

        [UxmlAttribute] public bool ApplyLeft { get; set; } = true;
        [UxmlAttribute] public bool ApplyTop { get; set; } = true;
        [UxmlAttribute] public bool ApplyRight { get; set; } = true;
        [UxmlAttribute] public bool ApplyBottom { get; set; } = true;

        public SafeAreaInsets Insets { get; private set; }

        public SafeAreaElement()
        {
            AddToClassList(UssClassName);
            pickingMode = PickingMode.Ignore;
            style.flexGrow = 1;
            RegisterCallback<AttachToPanelEvent>(_ => Refresh());
            RegisterCallback<GeometryChangedEvent>(_ => Refresh());
        }

        public void Refresh()
        {
            if (panel == null || panel.contextType != ContextType.Player)
            {
                return;
            }

            Rect panelRect = panel.visualTree.layout;
            if (float.IsNaN(panelRect.width) || panelRect.width <= 0f)
            {
                return;
            }

            SafeAreaInsets insets = SafeAreaInsets.Compute(Screen.safeArea, new Vector2(Screen.width, Screen.height), panelRect.size);
            Insets = insets;
            SetPadding(ApplyLeft ? insets.Left : 0f, ApplyTop ? insets.Top : 0f, ApplyRight ? insets.Right : 0f, ApplyBottom ? insets.Bottom : 0f);
        }

        private void SetPadding(float left, float top, float right, float bottom)
        {
            // Writing the same value again would trigger another GeometryChangedEvent.
            if (Mathf.Approximately(resolvedStyle.paddingLeft, left) && Mathf.Approximately(resolvedStyle.paddingTop, top)
                && Mathf.Approximately(resolvedStyle.paddingRight, right) && Mathf.Approximately(resolvedStyle.paddingBottom, bottom))
            {
                return;
            }

            style.paddingLeft = left;
            style.paddingTop = top;
            style.paddingRight = right;
            style.paddingBottom = bottom;
        }
    }
}
