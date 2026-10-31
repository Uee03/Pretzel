using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    public sealed class RegistrationDemo : IStartable
    {
        private const string Topic = "01 Registration";

        private readonly ISampleLog _log;
        private readonly Greeter _greeter;
        private readonly IScoreReader _reader;
        private readonly IScoreWriter _writer;
        private readonly IDiceRoller _dice;
        private readonly BuildInfo _build;

        public RegistrationDemo(ISampleLog log, Greeter greeter, IScoreReader reader, IScoreWriter writer, IDiceRoller dice, BuildInfo build)
        {
            _log = log;
            _greeter = greeter;
            _reader = reader;
            _writer = writer;
            _dice = dice;
            _build = build;
        }

        public void Start()
        {
            _log.Write(Topic, "WithParameter: " + _greeter.Greet("VContainer"));

            _writer.Add(25);
            _log.Write(Topic, $"Two interfaces, one instance: wrote 25, read {_reader.Score}, same object {ReferenceEquals(_reader, _writer)}");
            _log.Write(Topic, $"Registration delegate: rolled {_dice.Roll()}");
            _log.Write(Topic, $"RegisterInstance: version {_build.Version}");
        }
    }
}
