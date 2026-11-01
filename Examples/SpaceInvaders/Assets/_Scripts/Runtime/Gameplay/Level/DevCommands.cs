using System;
using Pretzel.DevConsole;
using VContainer.Unity;

namespace SpaceInvaders.Gameplay
{
    /// <summary>Registers the Pretzel dev console commands that change the running game. Press the back-quote key in Play mode.</summary>
    public sealed class DevCommands : IStartable, IDisposable
    {
        private static readonly string[] _names = { "god", "score", "lives", "wave", "clear", "ufo" };

        private readonly DeveloperConsole _console;
        private readonly IGameService _game;
        private readonly RunState _run;
        private readonly PlayerShip _player;
        private readonly InvaderFormation _formation;
        private readonly UfoSpawner _ufo;
        private readonly LevelPresenter _level;

        public DevCommands(DeveloperConsole console, IGameService game, RunState run, PlayerShip player, InvaderFormation formation, UfoSpawner ufo, LevelPresenter level)
        {
            _console = console;
            _game = game;
            _run = run;
            _player = player;
            _formation = formation;
            _ufo = ufo;
            _level = level;
        }

        public void Start()
        {
            CommandRegistry commands = _console.Commands;
            commands.Register("god", "Toggles invulnerability.", _ =>
            {
                _player.GodMode = !_player.GodMode;
                return _player.GodMode ? "God mode on." : "God mode off.";
            });
            commands.Register("score", "score <points>: adds points.", args =>
            {
                int points = args.Length > 0 ? int.Parse(args[0]) : 100;
                _game.AddScore(points);
                return $"Score is {_game.Score}.";
            });
            commands.Register("lives", "lives <count>: sets the lives left.", args =>
            {
                _run.SetLives(args.Length > 0 ? int.Parse(args[0]) : RunState.StartingLives);
                return $"Lives: {_run.Lives}.";
            });
            commands.Register("wave", "wave <number>: skips to a wave.", args =>
            {
                int wave = args.Length > 0 ? int.Parse(args[0]) : _run.Wave + 1;
                _level.JumpToWave(wave);
                return $"Wave {_run.Wave}.";
            });
            commands.Register("clear", "Destroys every invader.", _ =>
            {
                _formation.KillAll();
                return "Cleared.";
            });
            commands.Register("ufo", "Sends the mystery ship now.", _ =>
            {
                _ufo.LaunchNow();
                return "Mystery ship launched.";
            });
        }

        public void Dispose()
        {
            if (_console == null)
            {
                return;
            }

            foreach (string name in _names)
            {
                _console.Commands.Unregister(name);
            }
        }
    }
}
