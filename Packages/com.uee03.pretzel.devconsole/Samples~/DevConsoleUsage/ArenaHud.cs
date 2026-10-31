using UnityEngine;

namespace Pretzel.Samples.Console
{
    /// <summary>A text HUD with the numbers the console changes. IMGUI keeps the sample free of extra UI setup.</summary>
    [RequireComponent(typeof(ArenaGame))]
    public sealed class ArenaHud : MonoBehaviour
    {
        private ArenaGame _game;
        private GUIStyle _style;

        private void Awake()
        {
            _game = GetComponent<ArenaGame>();
        }

        private void OnGUI()
        {
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = Color.white } };
            PlayerHealth player = _game.Player;
            string state = player.IsDead ? "  DEAD (type revive)" : player.IsGodMode ? "  GOD MODE" : string.Empty;
            GUI.Label(new Rect(12f, 8f, 600f, 28f), $"HP {player.Current:0}/{player.Max:0}{state}", _style);
            GUI.Label(new Rect(12f, 32f, 600f, 28f), $"Gold {_game.Gold}   Kills {_game.Kills}   Enemies {_game.Enemies.Count}", _style);
            GUI.Label(new Rect(12f, 56f, 600f, 28f), "Move: WASD. Open the console with the back-quote key and type help.", _style);
        }
    }
}
