namespace Pretzel.DI
{
    /// <summary>An entry point called every frame from its scope's LateUpdate.</summary>
    public interface ILateTickable
    {
        void LateTick();
    }
}
