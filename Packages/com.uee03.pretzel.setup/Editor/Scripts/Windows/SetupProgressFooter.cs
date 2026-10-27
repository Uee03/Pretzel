using Pretzel.Setup.Addons.Services;
using UnityEditor;
using UnityEngine.UIElements;

namespace Pretzel.Setup.Windows
{
    /// <summary>
    /// The bar at the bottom of the setup window while a package is installed, updated or removed.
    /// Package Manager reports no percentage, so the bar is indeterminate.
    /// </summary>
    internal sealed class SetupProgressFooter
    {
        private const string HiddenClass = "pretzel-progress--hidden";
        private const double SweepSeconds = 1.4;
        private const float FillPercent = 30f;

        private readonly VisualElement _root;
        private readonly Label _label;
        private readonly VisualElement _fill;
        private readonly IVisualElementScheduledItem _animation;

        public SetupProgressFooter(VisualElement window)
        {
            _root = window.Q("setup-window__progress");
            _label = window.Q<Label>("setup-window__progress-label");
            _fill = window.Q("setup-window__progress-fill");
            _fill.style.width = Length.Percent(FillPercent);

            _animation = _root.schedule.Execute(Tick).Every(16);
            _animation.Pause();

            _root.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                PackageService.Changed += Refresh;
                Refresh();
            });
            _root.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                PackageService.Changed -= Refresh;
                _animation.Pause();
            });
        }

        private void Refresh()
        {
            bool busy = PackageService.IsBusy;
            _root.EnableInClassList(HiddenClass, !busy);
            if (busy)
            {
                _label.text = PackageService.BusyLabel + "...";
                _animation.Resume();
            }
            else
            {
                _animation.Pause();
            }
        }

        private void Tick()
        {
            float phase = (float)(EditorApplication.timeSinceStartup % SweepSeconds / SweepSeconds);
            _fill.style.left = Length.Percent(-FillPercent + phase * (100f + FillPercent));
        }
    }
}
