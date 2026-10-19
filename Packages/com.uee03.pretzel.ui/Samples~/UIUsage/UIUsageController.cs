using Pretzel.Tween;
using Pretzel.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.UI
{
    /// <summary>
    /// A screen controller. Add it next to a UIDocument. It builds a small panel in code, fades it with
    /// extension methods, and shows how a controller binds and unbinds its callbacks.
    /// </summary>
    public class UIUsageController : UIControllerBase
    {
        private Button _toggle;
        private VisualElement _panel;
        private bool _panelVisible = true;

        protected override void OnBind(VisualElement root)
        {
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.Center;

            _panel = UIToolkitUtility.CreateContainer(root);
            _panel.style.paddingTop = _panel.style.paddingBottom = 16;
            _panel.style.paddingLeft = _panel.style.paddingRight = 24;
            _panel.style.backgroundColor = new Color(0.1f, 0.1f, 0.12f, 0.9f);
            UIToolkitUtility.CreateLabel("Hello from Pretzel.UI", _panel);

            _toggle = UIToolkitUtility.CreateButton("Fade panel", null, root);
            _toggle.clicked += OnToggle;
        }

        protected override void OnUnbind(VisualElement root)
        {
            _toggle.clicked -= OnToggle;
        }

        private void OnToggle()
        {
            _panelVisible = !_panelVisible;
            if (_panelVisible)
            {
                _panel.FadeIn(0.4f, EaseType.OutCubic);
            }
            else
            {
                // The callback runs when the transition ends, or never if a newer fade replaces this one.
                _panel.FadeOut(0.4f, EaseType.InCubic, () => Debug.Log("Panel hidden"));
            }
        }
    }
}
