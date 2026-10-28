namespace Pretzel.Integrations.VContainer.Tests
{
    public sealed class Counter : ICounter
    {
        public Counter(TestSettings settings) => Settings = settings;

        public TestSettings Settings { get; }
    }
}
