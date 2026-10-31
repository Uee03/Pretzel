using Pretzel.DevConsole;
using UnityEngine;

namespace Pretzel.Samples.Console
{
    /// <summary>
    /// Connects the console to the arena: registers <see cref="ArenaCommands"/> and keeps typing from moving the player.
    /// Put it on the same GameObject as the <see cref="DeveloperConsole"/>.
    /// </summary>
    [RequireComponent(typeof(DeveloperConsole))]
    public sealed class GameCommands : MonoBehaviour
    {
        [Tooltip("Found in the scene when empty.")]
        [SerializeField] private ArenaGame _game;

        private DeveloperConsole _console;

        private void Start()
        {
            _console = GetComponent<DeveloperConsole>();
            if (!_game)
            {
                _game = FindAnyObjectByType<ArenaGame>();
            }

            if (!_game)
            {
                Debug.LogWarning("GameCommands needs an ArenaGame in the scene.", this);
                enabled = false;
                return;
            }

            new ArenaCommands(_game).Register(_console.Commands);
        }

        private void Update()
        {
            _game.InputBlocked = _console.IsOpen;
        }
    }
}
