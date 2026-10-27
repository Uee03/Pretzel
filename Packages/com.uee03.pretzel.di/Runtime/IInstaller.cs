namespace Pretzel.DI
{
    /// <summary>A reusable group of registrations.</summary>
    public interface IInstaller
    {
        void Install(ContainerBuilder builder);
    }
}
