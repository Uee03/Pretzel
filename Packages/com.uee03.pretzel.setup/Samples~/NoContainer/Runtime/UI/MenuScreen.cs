using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.NoContainer
{
    /// <summary>The title screen: best score, a volume setting stored in preferences, and the way into the game.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MenuScreen : MonoBehaviour
    {
        [SerializeField] private GameServices _services;

        private Label _best;
        private Label _rounds;
        private Button _play;

        private void OnEnable()
        {
            if (_services == null || !_services.IsInitialized)
            {
                Debug.LogError("MenuScreen needs the GameServices asset, initialized by GameServicesBootstrap.", this);
                return;
            }

            Build(GetComponent<UIDocument>().rootVisualElement);
            _services.Progress.Changed += ShowProgress;
            ShowProgress(_services.Progress.Current);
        }

        private void OnDisable()
        {
            if (_services != null && _services.IsInitialized)
            {
                _services.Progress.Changed -= ShowProgress;
                _services.Preferences.Save();
            }
        }

        private void Build(VisualElement root)
        {
            root.Clear();
            VisualElement screen = ScreenElements.Panel(root, "nc-screen");
            VisualElement card = ScreenElements.Panel(screen, "nc-card");

            ScreenElements.Text(card, "Click Rush", "nc-title");
            ScreenElements.Text(card, $"Click as often as you can in {_services.RoundSeconds:0} seconds.", "nc-hint");
            _best = ScreenElements.Text(card, string.Empty, "nc-stat");
            _rounds = ScreenElements.Text(card, string.Empty, "nc-stat", "nc-stat--minor");

            if (_services.Progress.IsSaveProtected)
            {
                ScreenElements.Text(card, "Your saved progress could not be read, so it is kept untouched and not saved this session.", "nc-warning");
            }

            _play = ScreenElements.Action(card, "Play", OnPlayClicked, "nc-button--primary");
            BuildAudioSettings(card);
        }

        private void BuildAudioSettings(VisualElement card)
        {
            Slider volume = new("Volume", 0f, 1f) { value = _services.Preferences.Current.audio.master };
            volume.AddToClassList("nc-slider");
            volume.RegisterValueChangedCallback(change => _services.Preferences.Modify(p => p.audio.master = change.newValue));
            card.Add(volume);

            Toggle mute = new("Mute") { value = _services.Preferences.Current.audio.muted };
            mute.AddToClassList("nc-toggle");
            mute.RegisterValueChangedCallback(change => _services.Preferences.Modify(p => p.audio.muted = change.newValue));
            card.Add(mute);
        }

        private void ShowProgress(GameProgress progress)
        {
            _best.text = $"Best score: {progress.bestScore}";
            _rounds.text = $"Rounds played: {progress.roundsPlayed}";
        }

        private async void OnPlayClicked()
        {
            _play.SetEnabled(false);
            try
            {
                await _services.Scenes.LoadAsync(SceneNames.Game);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (this != null)
                {
                    _play.SetEnabled(true);
                }
            }
        }
    }
}
