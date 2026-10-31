namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Created outside the container and registered as an instance.</summary>
    public sealed class BuildInfo
    {
        public BuildInfo(string version) => Version = version;

        public string Version { get; }
    }
}
