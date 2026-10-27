namespace Pretzel.DI
{
    /// <summary>An entry point called once after its scope is built.</summary>
    public interface IStartable
    {
        void Start();
    }
}
