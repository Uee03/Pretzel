using NUnit.Framework;
using Pretzel.Core.Timers;

namespace Pretzel.Core.Tests
{
    public sealed class TimerTests
    {
        [Test]
        public void CountdownCompletesOnceAndReportsProgress()
        {
            CountdownTimer timer = new(2f);
            int completed = 0;
            timer.Completed += () => completed++;

            timer.Tick(1f);
            Assert.That(timer.Remaining, Is.EqualTo(2f), "A timer that was not started does not tick.");

            timer.Start();
            timer.Tick(0.5f);
            Assert.That(timer.Progress, Is.EqualTo(0.25f).Within(0.0001f));
            timer.Tick(5f);
            timer.Tick(1f);
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(timer.IsFinished, Is.True);
            Assert.That(timer.IsRunning, Is.False);
        }

        [Test]
        public void CountdownPauseResumeAndReset()
        {
            CountdownTimer timer = new(1f);
            timer.Start();
            timer.Tick(0.25f);
            timer.Pause();
            timer.Tick(10f);
            Assert.That(timer.Remaining, Is.EqualTo(0.75f).Within(0.0001f));
            timer.Resume();
            timer.Tick(0.25f);
            Assert.That(timer.Remaining, Is.EqualTo(0.5f).Within(0.0001f));
            timer.Reset(3f);
            Assert.That(timer.Remaining, Is.EqualTo(3f));
            Assert.That(timer.IsRunning, Is.False);
        }

        [Test]
        public void ZeroDurationCountdownCompletesOnNextTick()
        {
            CountdownTimer timer = new(0f);
            bool completed = false;
            timer.Completed += () => completed = true;
            timer.Start();
            timer.Tick(0f);
            Assert.That(completed, Is.True);
        }

        [Test]
        public void CooldownGatesUse()
        {
            Cooldown cooldown = new(1f);
            Assert.That(cooldown.TryUse(), Is.True);
            Assert.That(cooldown.TryUse(), Is.False);
            cooldown.Tick(0.5f);
            Assert.That(cooldown.RemainingNormalized, Is.EqualTo(0.5f).Within(0.0001f));
            cooldown.Tick(0.6f);
            Assert.That(cooldown.IsReady, Is.True);
            Assert.That(cooldown.Remaining, Is.EqualTo(0f));
            Assert.That(cooldown.TryUse(), Is.True);
            cooldown.Reset();
            Assert.That(cooldown.IsReady, Is.True);
        }

        [Test]
        public void IntervalCatchesUpOnLongTicks()
        {
            IntervalTimer timer = new(0.5f);
            int elapsed = 0;
            timer.Elapsed += () => elapsed++;
            timer.Start();
            Assert.That(timer.Tick(1.75f), Is.EqualTo(3));
            Assert.That(timer.Tick(0.25f), Is.EqualTo(1));
            Assert.That(elapsed, Is.EqualTo(4));
        }

        [Test]
        public void IntervalStopsWhenAHandlerStopsIt()
        {
            IntervalTimer timer = new(1f);
            timer.Elapsed += timer.Stop;
            timer.Start();
            Assert.That(timer.Tick(5f), Is.EqualTo(1));
        }

        [Test]
        public void StopwatchOnlyCountsWhileRunning()
        {
            StopwatchTimer stopwatch = new();
            stopwatch.Tick(1f);
            stopwatch.Start();
            stopwatch.Tick(1.5f);
            stopwatch.Stop();
            stopwatch.Tick(1f);
            Assert.That(stopwatch.Elapsed, Is.EqualTo(1.5f));
            stopwatch.Restart();
            Assert.That(stopwatch.Elapsed, Is.EqualTo(0f));
            Assert.That(stopwatch.IsRunning, Is.True);
        }
    }
}
