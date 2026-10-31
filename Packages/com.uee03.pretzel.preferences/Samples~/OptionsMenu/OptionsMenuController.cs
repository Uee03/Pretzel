using System;
using System.Collections.Generic;
using Pretzel.Preferences;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.Preferences
{
    /// <summary>
    /// A runtime options screen built in code on a UIDocument. It edits a <see cref="PreferencesDraft"/> and only writes to the
    /// <see cref="IPreferencesService"/> on Apply, so Revert can drop the edits. Call <see cref="Bind"/> once the service is loaded.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class OptionsMenuController : MonoBehaviour
    {
        private const string Block = "options-menu";

        private static readonly string[] _languageCodes = { string.Empty, "en", "pt-BR", "de", "fr", "es", "ja" };
        private static readonly string[] _languageNames = { "System language", "English", "Portuguese (Brazil)", "German", "French", "Spanish", "Japanese" };
        private static readonly int[] _frameRates = { -1, 30, 60, 120, 144 };
        private static readonly string[] _frameRateNames = { "Platform default", "30", "60", "120", "144" };

        [SerializeField] private UIDocument _document;

        [Tooltip("Optional. Assign OptionsMenu.uss for the sample styling.")]
        [SerializeField] private StyleSheet _styleSheet;

        private readonly List<Action> _refreshers = new();
        private IPreferencesService _preferences;
        private PreferencesDraft _draft;
        private Button _apply;
        private Button _revert;
        private Label _status;

        private void Reset()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnDestroy()
        {
            if (_preferences != null)
            {
                _preferences.Changed -= OnPreferencesChanged;
            }
        }

        /// <summary>Shows the menu for the given service. Calling it again switches to another service.</summary>
        public void Bind(IPreferencesService preferences)
        {
            if (_preferences != null)
            {
                _preferences.Changed -= OnPreferencesChanged;
            }

            _preferences = preferences;
            _draft = new PreferencesDraft(preferences);
            _preferences.Changed += OnPreferencesChanged;
            Build();
        }

        private void Build()
        {
            if (!_document)
            {
                _document = GetComponent<UIDocument>();
            }

            VisualElement root = _document.rootVisualElement;
            root.Clear();
            _refreshers.Clear();
            if (_styleSheet)
            {
                root.styleSheets.Add(_styleSheet);
            }

            VisualElement panel = new();
            panel.AddToClassList(Block);
            root.Add(panel);

            Label title = new("Options");
            title.AddToClassList(Block + "__title");
            panel.Add(title);

            ScrollView scroll = new();
            scroll.AddToClassList(Block + "__scroll");
            panel.Add(scroll);

            BuildAudio(AddSection(scroll, "Audio"));
            BuildGraphics(AddSection(scroll, "Graphics"));
            BuildControls(AddSection(scroll, "Controls"));
            BuildLanguage(AddSection(scroll, "Language"));

            VisualElement buttons = new();
            buttons.AddToClassList(Block + "__buttons");
            _apply = AddButton(buttons, "Apply", OnApply);
            _revert = AddButton(buttons, "Revert", OnRevert);
            AddButton(buttons, "Reset to defaults", OnResetToDefaults);
            panel.Add(buttons);

            _status = new Label();
            _status.AddToClassList(Block + "__status");
            panel.Add(_status);

            RefreshControls();
        }

        private void BuildAudio(VisualElement section)
        {
            AddSlider(section, "Master", 0f, 1f, p => p.audio.master, (p, v) => p.audio.master = v);
            AddSlider(section, "Music", 0f, 1f, p => p.audio.music, (p, v) => p.audio.music = v);
            AddSlider(section, "Effects", 0f, 1f, p => p.audio.sfx, (p, v) => p.audio.sfx = v);
            AddSlider(section, "Interface", 0f, 1f, p => p.audio.ui, (p, v) => p.audio.ui = v);
            AddSlider(section, "Voice", 0f, 1f, p => p.audio.voice, (p, v) => p.audio.voice = v);
            AddToggle(section, "Mute all", p => p.audio.muted, (p, v) => p.audio.muted = v);
            AddToggle(section, "Subtitles", p => p.audio.subtitles, (p, v) => p.audio.subtitles = v);
        }

        private void BuildGraphics(VisualElement section)
        {
            List<string> qualities = new(QualitySettings.names);
            AddChoice(section, "Quality", qualities,
                p => p.graphics.qualityLevel < 0 ? QualitySettings.GetQualityLevel() : p.graphics.qualityLevel,
                (p, i) => p.graphics.qualityLevel = i);
            AddToggle(section, "VSync", p => p.graphics.vSyncCount > 0, (p, v) => p.graphics.vSyncCount = v ? 1 : 0);
            AddChoice(section, "Frame rate", new List<string>(_frameRateNames),
                p => Math.Max(0, Array.IndexOf(_frameRates, p.graphics.targetFrameRate)),
                (p, i) => p.graphics.targetFrameRate = _frameRates[i]);
        }

        private void BuildControls(VisualElement section)
        {
            AddSlider(section, "Look sensitivity", 0.05f, 10f, p => p.controls.lookSensitivity, (p, v) => p.controls.lookSensitivity = v);
            AddToggle(section, "Invert Y", p => p.controls.invertY, (p, v) => p.controls.invertY = v);
            AddToggle(section, "Vibration", p => p.controls.vibration, (p, v) => p.controls.vibration = v);
        }

        private void BuildLanguage(VisualElement section)
        {
            AddChoice(section, "Language", new List<string>(_languageNames),
                p => IndexOfLanguage(p.language),
                (p, i) => p.language = _languageCodes[i]);
        }

        private static int IndexOfLanguage(string code)
        {
            for (int i = 0; i < _languageCodes.Length; i++)
            {
                if (string.Equals(_languageCodes[i], code ?? string.Empty, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return 0;
        }

        private static VisualElement AddSection(VisualElement parent, string title)
        {
            GroupBox box = new(title);
            box.AddToClassList(Block + "__section");
            parent.Add(box);
            return box;
        }

        private static Button AddButton(VisualElement parent, string text, Action onClick)
        {
            Button button = new(onClick) { text = text };
            button.AddToClassList(Block + "__button");
            parent.Add(button);
            return button;
        }

        private void AddSlider(VisualElement parent, string label, float min, float max, Func<PlayerPreferences, float> get, Action<PlayerPreferences, float> set)
        {
            Slider slider = new(label, min, max) { showInputField = true };
            slider.RegisterValueChangedCallback(evt =>
            {
                set(_draft.Values, evt.newValue);
                UpdateButtons();
            });
            _refreshers.Add(() => slider.SetValueWithoutNotify(get(_draft.Values)));
            parent.Add(slider);
        }

        private void AddToggle(VisualElement parent, string label, Func<PlayerPreferences, bool> get, Action<PlayerPreferences, bool> set)
        {
            Toggle toggle = new(label);
            toggle.RegisterValueChangedCallback(evt =>
            {
                set(_draft.Values, evt.newValue);
                UpdateButtons();
            });
            _refreshers.Add(() => toggle.SetValueWithoutNotify(get(_draft.Values)));
            parent.Add(toggle);
        }

        private void AddChoice(VisualElement parent, string label, List<string> choices, Func<PlayerPreferences, int> getIndex, Action<PlayerPreferences, int> setIndex)
        {
            DropdownField field = new(label, choices, 0);
            field.RegisterValueChangedCallback(evt =>
            {
                setIndex(_draft.Values, field.index);
                UpdateButtons();
            });
            _refreshers.Add(() => field.SetValueWithoutNotify(choices[Mathf.Clamp(getIndex(_draft.Values), 0, choices.Count - 1)]));
            parent.Add(field);
        }

        private void RefreshControls()
        {
            foreach (Action refresh in _refreshers)
            {
                refresh();
            }

            UpdateButtons();
        }

        private void UpdateButtons()
        {
            bool dirty = _draft.IsDirty;
            _apply.SetEnabled(dirty);
            _revert.SetEnabled(dirty);
            _status.text = dirty ? "Unsaved changes." : string.Empty;
        }

        private void OnApply()
        {
            _draft.Commit();
            _preferences.ApplyGraphics();
            _status.text = "Saved.";
        }

        private void OnRevert()
        {
            _draft.Reset();
            RefreshControls();
        }

        private void OnResetToDefaults()
        {
            _preferences.ResetToDefaults(save: true);
            _preferences.ApplyGraphics();
        }

        // Another system changed the preferences, or Apply just committed: start a fresh draft from them.
        private void OnPreferencesChanged(PlayerPreferences preferences)
        {
            if (_draft == null || _apply == null)
            {
                return;
            }

            _draft.Reset();
            RefreshControls();
        }
    }
}
