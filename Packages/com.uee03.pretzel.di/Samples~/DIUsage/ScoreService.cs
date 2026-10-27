namespace Pretzel.Samples.DI
{
    /// <summary>A plain C# service. The container constructs it; it knows nothing about Unity or the container.</summary>
    public sealed class ScoreService
    {
        public int Score { get; private set; }

        public void Add(int points) => Score += points;
    }
}
