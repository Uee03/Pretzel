using System;
using Newtonsoft.Json;

namespace Pretzel.Setup.Composition
{
    /// <summary>A plain C# service the VContainer tab registers and, when its files are missing, creates.</summary>
    [Serializable]
    public sealed class ServiceDefinition
    {
        public string name = "NewService";
        public ServiceLifetime lifetime = ServiceLifetime.Singleton;
        public bool withInterface = true;
        public bool startable;
        public bool tickable;

        [JsonIgnore]
        public bool IsEntryPoint => startable || tickable;

        [JsonIgnore]
        public string InterfaceName => "I" + name;
    }
}
