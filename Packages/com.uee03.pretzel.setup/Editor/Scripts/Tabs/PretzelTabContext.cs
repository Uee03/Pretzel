using System;
using Pretzel.Setup.Settings;
using UnityEngine.UIElements;

namespace Pretzel.Setup.Tabs
{
    /// <summary>What a tab gets from the window: the settings, and a way to say they changed so they are saved.</summary>
    public sealed class PretzelTabContext
    {
        private readonly SettingsSaver _saver;

        public VisualElement WindowRoot { get; }

        public PretzelProjectSettings Project => PretzelSettings.Project;

        public PretzelUserSettings User => PretzelSettings.User;

        /// <summary>Raised when settings were reset, so tabs can rebind their fields.</summary>
        public event Action SettingsReset;

        internal PretzelTabContext(VisualElement windowRoot, SettingsSaver saver)
        {
            WindowRoot = windowRoot;
            _saver = saver;
        }

        public void MarkProjectDirty() => _saver.MarkProjectDirty();

        public void MarkUserDirty() => _saver.MarkUserDirty();

        public void RaiseSettingsReset() => SettingsReset?.Invoke();
    }
}
