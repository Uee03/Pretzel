using System;
using System.Collections.Generic;
using Pretzel.Tween;
using Pretzel.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace Pretzel.Samples.UI
{
    /// <summary>
    /// The main screen. Use UIUsage.uxml as the UIDocument's Source Asset. It fades a panel with the extension methods,
    /// drives its own <see cref="UIToolkitFader"/>, switches to an overlay screen and builds fields with
    /// <see cref="UIToolkitUtility"/>. Callbacks are registered in OnBind and removed in OnUnbind.
    /// </summary>
    public class UIUsageController : UIControllerBase
    {
        [SerializeField] private UIUsageOverlayController _overlay;
        [SerializeField, Min(0f)] private float _fadeDuration = 0.4f;

        private readonly List<string> _saveSlots = new() { "Slot 1 - Forest", "Slot 2 - Castle", "Slot 3 - Empty" };

        private VisualElement _panel;
        private EnumField _easeField;
        private ProgressBar _progress;
        private UIToolkitFader _progressFader;
        private bool _panelVisible = true;

        private Button _fadeButton;
        private Button _cancelButton;
        private Button _instantButton;
        private Button _pulseButton;
        private Button _overlayButton;
        private Button _blinkButton;

        private EaseType SelectedEase => _easeField != null ? (EaseType)_easeField.value : EaseType.OutCubic;

        protected override void OnBind(VisualElement root)
        {
            _panel = root.Q("panel");
            VisualElement fields = root.Q("fields");
            if (_panel == null || fields == null)
            {
                Debug.LogWarning("[Pretzel Samples] Assign UIUsage.uxml to the UIDocument's Source Asset.", this);
                return;
            }

            fields.Clear();
            BuildFields(fields);

            _fadeButton = Bind(root, "fade-button", OnFadeClicked);
            _cancelButton = Bind(root, "cancel-button", OnCancelClicked);
            _instantButton = Bind(root, "instant-button", OnInstantClicked);
            _pulseButton = Bind(root, "pulse-button", OnPulseClicked);
            _overlayButton = Bind(root, "overlay-button", OnOverlayClicked);
            _blinkButton = Bind(root, "blink-button", OnBlinkClicked);
        }

        protected override void OnUnbind(VisualElement root)
        {
            Unbind(_fadeButton, OnFadeClicked);
            Unbind(_cancelButton, OnCancelClicked);
            Unbind(_instantButton, OnInstantClicked);
            Unbind(_pulseButton, OnPulseClicked);
            Unbind(_overlayButton, OnOverlayClicked);
            Unbind(_blinkButton, OnBlinkClicked);

            // Stop running fades so no callback reaches a screen that is going away.
            _panel?.GetFader().Cancel();
            _progressFader?.Cancel();
            _progressFader = null;
        }

        protected override void OnShown() => Debug.Log("Main screen shown.", this);

        protected override void OnHidden() => Debug.Log("Main screen hidden.", this);

        private void OnFadeClicked()
        {
            _panelVisible = !_panelVisible;
            if (_panelVisible)
            {
                _panel.FadeIn(_fadeDuration, SelectedEase);
            }
            else
            {
                // The callback runs when the transition ends, or never if a newer fade or Cancel replaces this one.
                _panel.FadeOut(_fadeDuration, SelectedEase, () => Debug.Log("Panel hidden.", this));
            }
        }

        private void OnCancelClicked()
        {
            UIToolkitFader fader = _panel.GetFader();
            Debug.Log(fader.IsFading ? "Fade cancelled at the current opacity." : "No fade was running.", this);
            fader.Cancel();
        }

        private void OnInstantClicked()
        {
            _panelVisible = !_panelVisible;
            _panel.Fade(_panelVisible ? FadeType.FadeInNow : FadeType.FadeOutNow);
        }

        private void OnPulseClicked()
        {
            // A fader of your own. HideOnFadeOut false keeps the bar in the layout while it is transparent.
            _progressFader ??= new UIToolkitFader(_progress) { HideOnFadeOut = false };
            _progress.value = Random.Range(_progress.lowValue, _progress.highValue);
            _progressFader.Fade(FadeType.FadeOut, 0.2f, EaseType.InQuad, () => _progressFader?.FadeIn(0.3f, EaseType.OutQuad));
        }

        private void OnOverlayClicked()
        {
            if (_overlay)
            {
                // Shows the overlay and hides this screen.
                _overlay.SwitchTo(this);
            }
            else
            {
                Debug.LogWarning("[Pretzel Samples] Assign the overlay controller to open it.", this);
            }
        }

        private void OnBlinkClicked()
        {
            // ShowDelayed waits for the Show Delay set in the Inspector. Calling Show or Hide cancels it.
            Hide();
            ShowDelayed();
        }

        private void BuildFields(VisualElement fields)
        {
            ScrollView scroll = UIToolkitUtility.CreateScrollView(fields);
            scroll.style.maxHeight = 260f;

            _easeField = UIToolkitUtility.CreateEnumField("Fade ease", EaseType.OutCubic, scroll);
            Label easingMode = UIToolkitUtility.CreateLabel(string.Empty, scroll, null, "sample-hint");
            ShowEasingMode(easingMode, SelectedEase);
            _easeField.RegisterValueChangedCallback(evt => ShowEasingMode(easingMode, (EaseType)evt.newValue));

            Slider volume = UIToolkitUtility.CreateSlider("Volume", scroll);
            volume.lowValue = 0f;
            volume.highValue = 1f;
            volume.value = 0.8f;
            UIToolkitUtility.CreateToggle("Subtitles", scroll).value = true;
            _progress = UIToolkitUtility.CreateProgressBar("Loading", 0f, 100f, scroll);
            _progress.value = 40f;

            UIToolkitUtility.CreateLabel("Save slots (ListView)", scroll);
            ListView slots = UIToolkitUtility.CreateListView(scroll, null, "sample-list");
            slots.fixedItemHeight = 22f;
            slots.makeItem = () => new Label();
            slots.bindItem = (element, index) => ((Label)element).text = _saveSlots[index];
            slots.itemsSource = _saveSlots;

            UIToolkitUtility.CreateLabel("Settings (TreeView)", scroll);
            TreeView tree = UIToolkitUtility.CreateTreeView(scroll, null, "sample-list");
            tree.fixedItemHeight = 22f;
            tree.makeItem = () => new Label();
            tree.bindItem = (element, index) => ((Label)element).text = tree.GetItemDataForIndex<string>(index);
            tree.SetRootItems(new List<TreeViewItemData<string>>
            {
                new(1, "Audio", new List<TreeViewItemData<string>> { new(2, "Music"), new(3, "Effects") }),
                new(4, "Video")
            });
            tree.Rebuild();

            Foldout more = UIToolkitUtility.CreateFoldout("Every field factory", scroll);
            more.value = false;
            BuildEveryField(more);
        }

        private static void BuildEveryField(VisualElement parent)
        {
            UIToolkitUtility.CreateTextField("Player name", parent).value = "Ada";
            UIToolkitUtility.CreateIntegerField("Lives", parent).value = 3;
            UIToolkitUtility.CreateFloatField("Speed", parent).value = 4.5f;
            UIToolkitUtility.CreateDoubleField("Seed", parent);
            UIToolkitUtility.CreateLongField("Play time (ms)", parent);
            UIToolkitUtility.CreateUnsignedIntegerField("Coins", parent);
            UIToolkitUtility.CreateUnsignedLongField("Experience", parent);
            UIToolkitUtility.CreateHash128Field("Content hash", parent);
            UIToolkitUtility.CreateVector2Field("Aim", parent);
            UIToolkitUtility.CreateVector3Field("Spawn", parent);
            UIToolkitUtility.CreateVector4Field("Tint", parent);
            UIToolkitUtility.CreateVector2IntField("Grid cell", parent);
            UIToolkitUtility.CreateVector3IntField("Chunk", parent);
            UIToolkitUtility.CreateRectField("Safe area", parent);
            UIToolkitUtility.CreateRectIntField("Tile rect", parent);
            UIToolkitUtility.CreateBoundsField("Bounds", parent);
            UIToolkitUtility.CreateBoundsIntField("Room", parent);
            UIToolkitUtility.CreateSliderInt("Difficulty", parent).highValue = 5;
            UIToolkitUtility.CreateMinMaxSlider("Spawn range", 0f, 10f, parent);

            GroupBox quality = UIToolkitUtility.CreateGroupBox("Quality", parent);
            UIToolkitUtility.CreateRadioButton("Low", quality);
            UIToolkitUtility.CreateRadioButton("High", quality).value = true;

            // CreateElement covers any element type without a dedicated factory, such as MultiColumnListView.
            VisualElement row = UIToolkitUtility.CreateContainer(parent, null, "sample-row");
            UIToolkitUtility.CreateElement<Image>(row).style.width = 16f;
            UIToolkitUtility.CreateButton("Log", () => Debug.Log("Factory button clicked."), row);
        }

        private static void ShowEasingMode(Label label, EaseType ease)
        {
            // USS transitions support fewer curves, so ToEasingMode picks the closest one.
            label.text = $"{ease} runs as the USS easing '{ease.ToEasingMode()}'.";
        }

        private static Button Bind(VisualElement root, string name, Action onClick)
        {
            Button button = root.Q<Button>(name);
            if (button != null)
            {
                button.clicked += onClick;
            }

            return button;
        }

        private static void Unbind(Button button, Action onClick)
        {
            if (button != null)
            {
                button.clicked -= onClick;
            }
        }
    }
}
