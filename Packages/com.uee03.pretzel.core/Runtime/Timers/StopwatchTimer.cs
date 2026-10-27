namespace Pretzel.Core.Timers
{
    /// <summary>Accumulates elapsed time while running. The owner calls <see cref="Tick"/> with a delta time.</summary>
    public sealed class StopwatchTimer
    {
        public float Elapsed { get; private set; }

        public bool IsRunning { get; private set; }

        public void Start() => IsRunning = true;

        public void Stop() => IsRunning = false;

        public void Reset()
        {
            IsRunning = false;
            Elapsed = 0f;
        }

        public void Restart()
        {
            Elapsed = 0f;
            IsRunning = true;
        }

        public void Tick(float deltaTime)
        {
            if (IsRunning)
            {
                Elapsed += deltaTime;
            }
        }
    }
}
