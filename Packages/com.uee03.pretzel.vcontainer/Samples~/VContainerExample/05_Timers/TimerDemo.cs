using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    public sealed class TimerDemo : IStartable
    {
        private const string Topic = "05 Timers";

        private readonly ISampleLog _log;
        private readonly ITimerService _timers;
        private Timer _countdown;
        private int _count = 3;

        public TimerDemo(ISampleLog log, ITimerService timers)
        {
            _log = log;
            _timers = timers;
        }

        public void Start()
        {
            _timers.Start(2f, () => _log.Write(Topic, "One-shot timer: two seconds passed"));
            _countdown = _timers.Start(1f, CountDown, repeat: true);
        }

        private void CountDown()
        {
            _log.Write(Topic, $"Repeating timer: {_count}");
            _count--;
            if (_count == 0)
            {
                _countdown.Cancel();
                _log.Write(Topic, "Repeating timer cancelled");
            }
        }
    }
}
