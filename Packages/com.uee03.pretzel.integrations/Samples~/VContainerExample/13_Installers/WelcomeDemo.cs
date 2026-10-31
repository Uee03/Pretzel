using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    public sealed class WelcomeDemo : IStartable
    {
        private readonly ISampleLog _log;
        private readonly WelcomeMessage _message;

        public WelcomeDemo(ISampleLog log, WelcomeMessage message)
        {
            _log = log;
            _message = message;
        }

        public void Start() => _log.Write("13 Installers", _message.Text);
    }
}
