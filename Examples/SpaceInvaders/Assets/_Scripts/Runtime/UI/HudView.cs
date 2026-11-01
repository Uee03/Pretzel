using System;
using System.Collections.Generic;
using Pretzel.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpaceInvaders.UI
{
    /// <summary>The in-game screen: score bar, pause overlay, game-over overlay and toasts. It only shows what presenters tell it.</summary>
    public sealed class HudView : UIControllerBase
    {
        private readonly List<IDisposable> _bindings = new();
        private Label _score;
        private Label _highScore;
        private Label _wave;
        private Label _lives;
        private Label _finalScore;
        private Label _finalHighScore;
        private Label _newHighScore;
        private Slider _volume;
        private VisualElement _pause;
        private VisualElement _gameOver;
        private UIScreenStack _overlays;
        private ToastService _toasts;

        public event Action Bound;

        public event Action ResumeRequested;

        public event Action RetryRequested;

        public event Action QuitRequested;

        public event Action<float> VolumeChanged;

        public bool IsPauseVisible => _overlays != null && _pause != null && _overlays.Current == _pause;

        protected override void OnBind(VisualElement root)
        {
            _score = root.QRequired<Label>("score");
            _highScore = root.QRequired<Label>("high-score");
            _wave = root.QRequired<Label>("wave");
            _lives = root.QRequired<Label>("lives");
            _pause = root.QRequired<VisualElement>("pause");
            _gameOver = root.QRequired<VisualElement>("game-over");
            _finalScore = root.QRequired<Label>("game-over-score");
            _finalHighScore = root.QRequired<Label>("game-over-high-score");
            _newHighScore = root.QRequired<Label>("game-over-new-high");
            _volume = root.QRequired<Slider>("pause-volume");
            _toasts = new ToastService(root.QRequired<VisualElement>("toasts"));

            _pause.FadeOutNow();
            _gameOver.FadeOutNow();
            _overlays = new UIScreenStack(0.15f);

            _bindings.Add(root.QRequired<Button>("pause-resume").OnClick(() => ResumeRequested?.Invoke()));
            _bindings.Add(root.QRequired<Button>("pause-quit").OnClick(() => QuitRequested?.Invoke()));
            _bindings.Add(root.QRequired<Button>("game-over-retry").OnClick(() => RetryRequested?.Invoke()));
            _bindings.Add(root.QRequired<Button>("game-over-quit").OnClick(() => QuitRequested?.Invoke()));
            _bindings.Add(_volume.OnValueChanged(evt => VolumeChanged?.Invoke(evt.newValue)));
            Bound?.Invoke();
        }

        protected override void OnUnbind(VisualElement root)
        {
            foreach (IDisposable binding in _bindings)
            {
                binding.Dispose();
            }

            _bindings.Clear();
            _toasts?.Clear();
            _overlays = null;
        }

        public void SetScore(int score)
        {
            if (_score != null)
            {
                _score.text = score.ToString("D6");
            }
        }

        public void SetHighScore(int highScore)
        {
            if (_highScore != null)
            {
                _highScore.text = highScore.ToString("D6");
            }
        }

        public void SetWave(int wave)
        {
            if (_wave != null)
            {
                _wave.text = wave.ToString();
            }
        }

        public void SetLives(int lives)
        {
            if (_lives != null)
            {
                _lives.text = lives.ToString();
            }
        }

        public void SetVolume(float volume)
        {
            _volume?.SetValueWithoutNotify(volume);
        }

        public void ShowToast(string message, ToastKind kind = ToastKind.Info, float seconds = 1.8f)
        {
            _toasts?.Show(message, seconds, kind);
        }

        public void ShowPause()
        {
            if (_overlays != null && !_overlays.Contains(_pause))
            {
                _overlays.Push(_pause, modal: true);
            }
        }

        public void HidePause()
        {
            if (IsPauseVisible)
            {
                _overlays.Pop(keep: 0);
            }
        }

        public void ShowGameOver(int score, int highScore, bool isNewHighScore)
        {
            if (_overlays == null)
            {
                return;
            }

            _finalScore.text = score.ToString("D6");
            _finalHighScore.text = highScore.ToString("D6");
            _newHighScore.SetDisplayed(isNewHighScore);
            if (_overlays.Contains(_pause))
            {
                _overlays.Pop(keep: 0);
            }

            if (!_overlays.Contains(_gameOver))
            {
                _overlays.Push(_gameOver, modal: true);
            }
        }
    }
}
