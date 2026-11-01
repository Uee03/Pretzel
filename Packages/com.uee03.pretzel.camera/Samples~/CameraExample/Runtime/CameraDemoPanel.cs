using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Cameras.Samples
{
    /// <summary>
    /// A small control panel for the sample: buttons that add trauma and a button that cycles the fit mode, with a line
    /// that shows the aspect ratio and the resulting orthographic size. Resize the Game view to see the fit.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class CameraDemoPanel : MonoBehaviour
    {
        private static readonly FitMode[] _modes = { FitMode.Contain, FitMode.Cover, FitMode.Width, FitMode.Height };

        [SerializeField] private CameraShake _shake;
        [SerializeField] private PlayfieldFitter _fitter;

        private Label _info;
        private Button _fitButton;
        private IVisualElementScheduledItem _refresh;
        private IVisualElementScheduledItem _rumble;
        private int _modeIndex;

        private void OnEnable()
        {
            if (_shake == null || _fitter == null)
            {
                Debug.LogError("CameraDemoPanel needs the CameraShake and the PlayfieldFitter.", this);
                return;
            }

            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            VisualElement bar = new();
            bar.AddToClassList("cd-bar");
            root.Add(bar);

            AddButton(bar, "Tap", () => _shake.AddTrauma(0.25f));
            AddButton(bar, "Hit", () => _shake.AddTrauma(0.5f));
            AddButton(bar, "Boom", () => _shake.AddTrauma(0.9f));
            Toggle rumble = new("Rumble");
            rumble.AddToClassList("cd-toggle");
            rumble.RegisterValueChangedCallback(change => SetRumble(bar, change.newValue));
            bar.Add(rumble);
            _fitButton = AddButton(bar, string.Empty, CycleMode);

            _info = new Label();
            _info.AddToClassList("cd-info");
            root.Add(_info);

            _modeIndex = Array.IndexOf(_modes, _fitter.Mode);
            if (_modeIndex < 0)
            {
                _modeIndex = 0;
            }

            ShowMode();
            _refresh = _info.schedule.Execute(ShowInfo).Every(250);
        }

        private void OnDisable()
        {
            _refresh?.Pause();
            _rumble?.Pause();
        }

        private static Button AddButton(VisualElement parent, string text, Action onClick)
        {
            Button button = new(onClick) { text = text };
            button.AddToClassList("cd-button");
            parent.Add(button);
            return button;
        }

        // A rumble tops trauma up to a low ceiling, so it never drowns out a hit.
        private void SetRumble(VisualElement host, bool on)
        {
            _rumble?.Pause();
            if (on)
            {
                _rumble = host.schedule.Execute(() => _shake.AddTraumaUpTo(0.08f, 0.25f)).Every(60);
            }
        }

        private void CycleMode()
        {
            _modeIndex = (_modeIndex + 1) % _modes.Length;
            _fitter.Configure(_fitter.Playfield, _modes[_modeIndex]);
            ShowMode();
        }

        private void ShowMode()
        {
            _fitButton.text = $"Fit: {_modes[_modeIndex]}";
        }

        private void ShowInfo()
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                _info.text = $"Aspect {camera.aspect:0.00}   Orthographic size {camera.orthographicSize:0.00}   Trauma {_shake.Trauma:0.00}";
            }
        }
    }
}
