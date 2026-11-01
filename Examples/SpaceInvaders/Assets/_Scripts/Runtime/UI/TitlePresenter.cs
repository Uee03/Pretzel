using System;
using SpaceInvaders.Audio;
using VContainer.Unity;

namespace SpaceInvaders.UI
{
    /// <summary>Wires the title screen to the game services: the saved high score, the volume and the way into the game.</summary>
    public sealed class TitlePresenter : IStartable, IDisposable
    {
        private readonly TitleView _view;
        private readonly IGameService _game;
        private readonly IGameNavigator _navigator;
        private readonly MasterVolumeSetting _volume;

        public TitlePresenter(TitleView view, IGameService game, IGameNavigator navigator, MasterVolumeSetting volume)
        {
            _view = view;
            _game = game;
            _navigator = navigator;
            _volume = volume;
        }

        public void Start()
        {
            _view.PlayRequested += OnPlay;
            _view.QuitRequested += OnQuit;
            _view.VolumeChanged += _volume.Set;
            _view.Bound += Refresh;

            // The game flow loads preferences and then progress at startup; show both as soon as they are there.
            _game.ProgressLoaded += Refresh;
            Refresh();
        }

        public void Dispose()
        {
            _view.PlayRequested -= OnPlay;
            _view.QuitRequested -= OnQuit;
            _view.VolumeChanged -= _volume.Set;
            _view.Bound -= Refresh;
            _game.ProgressLoaded -= Refresh;
        }

        private void Refresh()
        {
            _view.SetHighScore(_game.HighScore);
            _view.SetVolume(_volume.Value);
        }

        private void OnPlay()
        {
            _ = _navigator.PlayAsync();
        }

        private static void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }
    }
}
