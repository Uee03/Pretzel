using System;

namespace Pretzel.Samples.VContainerExample
{
    public interface ITimerService
    {
        bool Paused { get; set; }

        Timer Start(float seconds, Action elapsed, bool repeat = false);
    }
}
