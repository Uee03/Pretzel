using System.Globalization;
using Pretzel.DevConsole;
using UnityEngine;

namespace Pretzel.Samples.Console
{
    /// <summary>Registers gameplay commands on a DeveloperConsole in the same GameObject.</summary>
    [RequireComponent(typeof(DeveloperConsole))]
    public sealed class GameCommands : MonoBehaviour
    {
        private bool _godMode;

        private void Start()
        {
            CommandRegistry commands = GetComponent<DeveloperConsole>().Commands;

            commands.Register("timescale", "timescale <value> - sets Time.timeScale.", args =>
            {
                if (args.Length == 1 && float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float scale) && scale >= 0f)
                {
                    Time.timeScale = scale;
                    return $"Time scale is {scale}.";
                }

                return "Usage: timescale <value>";
            });

            commands.Register("god", "Toggles god mode.", _ =>
            {
                _godMode = !_godMode;
                return _godMode ? "God mode on." : "God mode off.";
            });

            commands.Register("fps", "Shows the current frame rate.", _ => $"{1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime):0} fps");
        }
    }
}
