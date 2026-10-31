using System;

namespace Pretzel.Cloud.Tests
{
    /// <summary>A UTC clock a test moves by hand.</summary>
    internal sealed class TestClock
    {
        public DateTime Now { get; set; } = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        public DateTime Read() => Now;

        public void Advance(TimeSpan span) => Now += span;
    }
}
