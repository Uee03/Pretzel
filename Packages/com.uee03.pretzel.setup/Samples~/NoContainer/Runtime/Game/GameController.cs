using UnityEngine;

namespace Pretzel.Samples.NoContainer
{
    /// <summary>Runs one round and publishes its progress on the event bus. It never touches the UI.</summary>
    public sealed class GameController : MonoBehaviour
    {
        [SerializeField] private GameServices _services;

        private GameRound _round;
        private int _lastSeconds;
        private bool _running;

        public GameRound Round => _round;

        private void OnEnable()
        {
            if (_services == null || !_services.IsInitialized)
            {
                Debug.LogError("GameController needs the GameServices asset, initialized by GameServicesBootstrap.", this);
                enabled = false;
                return;
            }

            StartRound();
        }

        private void Update()
        {
            if (!_running)
            {
                return;
            }

            if (_round.Tick(Time.deltaTime))
            {
                Finish();
            }
            else
            {
                PublishTimeIfChanged();
            }
        }

        /// <summary>Called by the screen's button. Ignored once the round is over.</summary>
        public void Click()
        {
            if (_running && _round.Click())
            {
                _services.Events.Publish(new ScoreChanged(_round.Score));
            }
        }

        public void StartRound()
        {
            _round = new GameRound(_services.RoundSeconds);
            _lastSeconds = _round.SecondsRemaining;
            _running = true;
            _services.Events.Publish(new ScoreChanged(0));
            _services.Events.Publish(new TimeChanged(_lastSeconds));
        }

        private void PublishTimeIfChanged()
        {
            int seconds = _round.SecondsRemaining;
            if (seconds != _lastSeconds)
            {
                _lastSeconds = seconds;
                _services.Events.Publish(new TimeChanged(seconds));
            }
        }

        private void Finish()
        {
            _running = false;
            bool isBest = _services.Progress.RecordRound(_round.Score);
            _services.Progress.Save();
            _services.Events.Publish(new TimeChanged(0));
            _services.Events.Publish(new RoundFinished(_round.Score, isBest));
        }
    }
}
