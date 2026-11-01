using System;
using System.Collections.Generic;
using Pretzel.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpaceInvaders.Coop
{
    /// <summary>The co-op screen: a score bar for each player, the wave, toasts, a leave button and the game-over panel. It only shows what the presenter tells it.</summary>
    public sealed class CoopHudView : UIControllerBase
    {
        public const int PlayerPanels = CoopRunState.MaxPlayers;

        private readonly List<IDisposable> _bindings = new();
        private readonly VisualElement[] _players = new VisualElement[PlayerPanels];
        private readonly Label[] _names = new Label[PlayerPanels];
        private readonly Label[] _scores = new Label[PlayerPanels];
        private readonly Label[] _lives = new Label[PlayerPanels];
        private readonly Label[] _results = new Label[PlayerPanels];
        private VisualElement _bar;
        private VisualElement _gameOver;
        private Label _wave;
        private Label _resultLine;
        private Label _waitHint;
        private Button _lobbyButton;
        private UIScreenStack _overlays;
        private ToastService _toasts;

        public event Action Bound;

        public event Action LeaveRequested;

        public event Action BackToLobbyRequested;

        protected override void OnBind(VisualElement root)
        {
            _bar = root.QRequired<VisualElement>("bar");
            _wave = root.QRequired<Label>("wave");
            _gameOver = root.QRequired<VisualElement>("game-over");
            _resultLine = root.QRequired<Label>("game-over-result");
            _waitHint = root.QRequired<Label>("game-over-wait");
            _lobbyButton = root.QRequired<Button>("game-over-lobby");
            _toasts = new ToastService(root.QRequired<VisualElement>("toasts"));
            for (int i = 0; i < PlayerPanels; i++)
            {
                string prefix = "player-" + (i + 1);
                _players[i] = root.QRequired<VisualElement>(prefix);
                _names[i] = root.QRequired<Label>(prefix + "-name");
                _scores[i] = root.QRequired<Label>(prefix + "-score");
                _lives[i] = root.QRequired<Label>(prefix + "-lives");
                _results[i] = root.QRequired<Label>("game-over-" + prefix);
            }

            _bar.SetDisplayed(false);
            _gameOver.FadeOutNow();
            _overlays = new UIScreenStack(0.15f);
            _bindings.Add(root.QRequired<Button>("leave").OnClick(() => LeaveRequested?.Invoke()));
            _bindings.Add(root.QRequired<Button>("game-over-leave").OnClick(() => LeaveRequested?.Invoke()));
            _bindings.Add(_lobbyButton.OnClick(() => BackToLobbyRequested?.Invoke()));
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

        public void SetBarVisible(bool visible)
        {
            _bar?.SetDisplayed(visible);
        }

        public void SetWave(int wave)
        {
            if (_wave != null)
            {
                _wave.text = Mathf.Max(1, wave).ToString();
            }
        }

        /// <param name="slot">0 or 1. Other slots have no panel and are ignored.</param>
        public void SetPlayer(int slot, int score, int lives, bool isYou)
        {
            if (slot < 0 || slot >= PlayerPanels || _players[slot] == null)
            {
                return;
            }

            _players[slot].SetDisplayed(true);
            _names[slot].text = isYou ? $"PLAYER {slot + 1} (YOU)" : $"PLAYER {slot + 1}";
            _scores[slot].text = score.ToString("D6");
            _lives[slot].text = lives > 0 ? $"LIVES {lives}" : "OUT";
        }

        public void HidePlayer(int slot)
        {
            if (slot >= 0 && slot < PlayerPanels && _players[slot] != null)
            {
                _players[slot].SetDisplayed(false);
            }
        }

        public void ShowToast(string message, ToastKind kind = ToastKind.Info, float seconds = 1.8f)
        {
            _toasts?.Show(message, seconds, kind);
        }

        /// <param name="winnerSlot">The slot with the highest score, or -1 for a draw or one player.</param>
        /// <param name="isHost">Only the host can send everyone back to the lobby.</param>
        public void ShowGameOver(int winnerSlot, bool isHost)
        {
            if (_overlays == null)
            {
                return;
            }

            _resultLine.text = winnerSlot >= 0 ? $"PLAYER {winnerSlot + 1} LEADS" : "A DRAW";
            _lobbyButton.SetDisplayed(isHost);
            _waitHint.SetDisplayed(!isHost);
            if (!_overlays.Contains(_gameOver))
            {
                _overlays.Push(_gameOver, modal: true);
            }
        }

        public void SetResult(int slot, int score)
        {
            if (slot >= 0 && slot < PlayerPanels && _results[slot] != null)
            {
                _results[slot].text = $"PLAYER {slot + 1}   {score:D6}";
                _results[slot].SetDisplayed(true);
            }
        }

        public void HideResult(int slot)
        {
            if (slot >= 0 && slot < PlayerPanels && _results[slot] != null)
            {
                _results[slot].SetDisplayed(false);
            }
        }

        public void HideGameOver()
        {
            if (_overlays != null && _overlays.Contains(_gameOver))
            {
                _overlays.Pop(keep: 0);
            }
        }
    }
}
