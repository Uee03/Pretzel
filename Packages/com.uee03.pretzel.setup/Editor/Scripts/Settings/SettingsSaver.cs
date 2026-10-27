using UnityEngine.UIElements;

namespace Pretzel.Setup.Settings
{
    /// <summary>Writes the settings a moment after the last change, and immediately on <see cref="Flush"/>.</summary>
    internal sealed class SettingsSaver
    {
        private const long DelayMs = 500;

        private readonly VisualElement _host;
        private IVisualElementScheduledItem _timer;
        private bool _projectDirty;
        private bool _userDirty;

        public SettingsSaver(VisualElement host)
        {
            _host = host;
        }

        public void MarkProjectDirty()
        {
            _projectDirty = true;
            Restart();
        }

        public void MarkUserDirty()
        {
            _userDirty = true;
            Restart();
        }

        public void Flush()
        {
            _timer?.Pause();
            _timer = null;

            if (_projectDirty)
            {
                _projectDirty = false;
                PretzelSettings.SaveProject();
            }

            if (_userDirty)
            {
                _userDirty = false;
                PretzelSettings.SaveUser();
            }
        }

        private void Restart()
        {
            _timer?.Pause();
            _timer = _host.schedule.Execute(Flush).StartingIn(DelayMs);
        }
    }
}
