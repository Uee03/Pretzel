using Pretzel.Preferences;

namespace Pretzel.Samples.Preferences
{
    /// <summary>Pushes one slice of the preferences into the game, for example a mixer, the listener or a language setting.</summary>
    public interface IPreferenceApplier
    {
        void Apply(PlayerPreferences preferences);
    }
}
