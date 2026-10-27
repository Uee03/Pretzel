using System;
using Pretzel.Data;

namespace Pretzel.Preferences
{
    /// <summary>Everything a player can change in an options menu. Add a migration when you rename or remove fields.</summary>
    [Serializable, SchemaVersion(1)]
    public sealed class PlayerPreferences
    {
        public AudioPreferences audio = new();
        public GraphicsPreferences graphics = new();
        public ControlPreferences controls = new();

        /// <summary>A language code such as "en" or "pt-BR". Empty follows the system language.</summary>
        public string language = string.Empty;

        public PlayerPreferences Clone()
        {
            return new PlayerPreferences
            {
                audio = (audio ?? new AudioPreferences()).Clone(),
                graphics = (graphics ?? new GraphicsPreferences()).Clone(),
                controls = (controls ?? new ControlPreferences()).Clone(),
                language = language ?? string.Empty
            };
        }
    }
}
