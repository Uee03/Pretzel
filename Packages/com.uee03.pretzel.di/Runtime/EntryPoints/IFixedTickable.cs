namespace Pretzel.DI
{
    /// <summary>An entry point called from its scope's FixedUpdate.</summary>
    public interface IFixedTickable
    {
        void FixedTick();
    }
}
