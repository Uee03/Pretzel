using System;
using System.Collections.Generic;
using Pretzel.UI;
using UnityEngine.UIElements;

namespace SpaceInvaders.UI
{
    /// <summary>The title screen: play, volume and quit, with the saved high score.</summary>
    public sealed class TitleView : UIControllerBase
    {
        private readonly List<IDisposable> _bindings = new();
        private Label _highScore;
        private Slider _volume;
        private Button _play;

        public event Action Bound;

        public event Action PlayRequested;

        public event Action CoopRequested;

        public event Action QuitRequested;

        public event Action<float> VolumeChanged;

        protected override void OnBind(VisualElement root)
        {
            _highScore = root.QRequired<Label>("high-score");
            _volume = root.QRequired<Slider>("volume");
            _play = root.QRequired<Button>("play");
            _bindings.Add(_play.OnClick(() => PlayRequested?.Invoke()));
            _bindings.Add(root.QRequired<Button>("coop").OnClick(() => CoopRequested?.Invoke()));
            _bindings.Add(root.QRequired<Button>("quit").OnClick(() => QuitRequested?.Invoke()));
            _bindings.Add(_volume.OnValueChanged(evt => VolumeChanged?.Invoke(evt.newValue)));
            _play.Focus();
            Bound?.Invoke();
        }

        protected override void OnUnbind(VisualElement root)
        {
            foreach (IDisposable binding in _bindings)
            {
                binding.Dispose();
            }

            _bindings.Clear();
        }

        public void SetHighScore(int highScore)
        {
            if (_highScore != null)
            {
                _highScore.text = highScore.ToString("D6");
            }
        }

        public void SetVolume(float volume)
        {
            _volume?.SetValueWithoutNotify(volume);
        }
    }
}
