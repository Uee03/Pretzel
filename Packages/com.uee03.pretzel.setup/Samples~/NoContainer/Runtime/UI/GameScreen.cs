using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.NoContainer
{
    /// <summary>The in-game screen. It listens to the event bus for what to show and tells the controller what the player did.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameScreen : MonoBehaviour
    {
        private const string HiddenClass = "nc-hidden";

        [SerializeField] private GameServices _services;
        [SerializeField] private GameController _controller;

        private readonly List<IDisposable> _subscriptions = new();
        private Label _score;
        private Label _time;
        private VisualElement _result;
        private Label _resultTitle;
        private Label _resultScore;

        private void OnEnable()
        {
            if (_services == null || !_services.IsInitialized || _controller == null)
            {
                Debug.LogError("GameScreen needs the GameServices asset and a GameController.", this);
                return;
            }

            Build(GetComponent<UIDocument>().rootVisualElement);
            _subscriptions.Add(_services.Events.Subscribe<ScoreChanged>(OnScoreChanged));
            _subscriptions.Add(_services.Events.Subscribe<TimeChanged>(OnTimeChanged));
            _subscriptions.Add(_services.Events.Subscribe<RoundFinished>(OnRoundFinished));
            ShowCurrentRound();
        }

        private void OnDisable()
        {
            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
        }

        private void Build(VisualElement root)
        {
            root.Clear();
            VisualElement screen = ScreenElements.Panel(root, "nc-screen");

            VisualElement bar = ScreenElements.Panel(screen, "nc-bar");
            _score = ScreenElements.Text(bar, "Score: 0", "nc-stat");
            _time = ScreenElements.Text(bar, "Time: 0", "nc-stat");

            ScreenElements.Action(screen, "Click!", _controller.Click, "nc-button--primary", "nc-button--huge");

            _result = ScreenElements.Panel(screen, "nc-card", "nc-result", HiddenClass);
            _resultTitle = ScreenElements.Text(_result, string.Empty, "nc-title");
            _resultScore = ScreenElements.Text(_result, string.Empty, "nc-stat");
            ScreenElements.Action(_result, "Play again", OnPlayAgainClicked, "nc-button--primary");
            ScreenElements.Action(_result, "Menu", OnMenuClicked);
        }

        // The controller may have started before this screen was enabled, so show its current state instead of waiting for a message.
        private void ShowCurrentRound()
        {
            GameRound round = _controller.Round;
            if (round != null)
            {
                _score.text = $"Score: {round.Score}";
                _time.text = $"Time: {round.SecondsRemaining}";
            }
        }

        private void OnScoreChanged(ScoreChanged message)
        {
            _score.text = $"Score: {message.Score}";
        }

        private void OnTimeChanged(TimeChanged message)
        {
            _time.text = $"Time: {message.SecondsRemaining}";
        }

        private void OnRoundFinished(RoundFinished message)
        {
            _resultTitle.text = message.IsBestScore ? "New best!" : "Time is up";
            _resultScore.text = $"You scored {message.Score}";
            _result.RemoveFromClassList(HiddenClass);
        }

        private void OnPlayAgainClicked()
        {
            _result.AddToClassList(HiddenClass);
            _controller.StartRound();
        }

        private async void OnMenuClicked()
        {
            try
            {
                await _services.Scenes.LoadAsync(SceneNames.Menu);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
