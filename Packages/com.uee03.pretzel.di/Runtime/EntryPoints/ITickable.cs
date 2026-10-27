namespace Pretzel.DI
{
    /// <summary>An entry point called every frame from its scope's Update.</summary>
    public interface ITickable
    {
        void Tick();
    }
}
