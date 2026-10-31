using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    public sealed class ScriptableInstallerDemo : IStartable
    {
        private readonly ISampleLog _log;

        public ScriptableInstallerDemo(ISampleLog log) => _log = log;

        public void Start() => _log.Write("13 Installers", "Registered by a ScriptableInstaller asset");
    }
}
