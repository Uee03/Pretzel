using System.Collections.Generic;
using Pretzel.Setup.Settings;

namespace Pretzel.Setup.Addons.Catalog
{
    /// <summary>One Pretzel module, as listed in the generated <c>Editor/Catalog/modules.json</c>.</summary>
    public sealed class ModuleInfo
    {
        public string Name { get; }
        public string DisplayName { get; }
        public string Description { get; }

        /// <summary>Other Pretzel modules this one needs, as package names.</summary>
        public IReadOnlyList<string> Dependencies { get; }

        /// <summary>Catalog and Unity packages the module cannot work without, as package names.</summary>
        public IReadOnlyList<string> Packages { get; }

        /// <summary>Third-party packages the module adapts. Each adapter assembly compiles only when its package is installed.</summary>
        public IReadOnlyList<(string DisplayName, string PackageName)> OptionalLibraries { get; }

        public ModuleInfo(string name, string displayName, string description, string[] dependencies,
            (string DisplayName, string PackageName)[] optionalLibraries = null, string[] packages = null)
        {
            Name = name;
            DisplayName = displayName;
            Description = description;
            Dependencies = dependencies;
            OptionalLibraries = optionalLibraries ?? new (string, string)[0];
            Packages = packages ?? new string[0];
        }

        /// <summary>The git identifier for this module, or null when no module repository is known.</summary>
        public string GetIdentifier(PretzelProjectSettings settings)
        {
            (string baseUrl, string version) = ModuleCatalog.ResolveSource(settings);
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return null;
            }

            string url = baseUrl + "?path=Packages/" + Name;
            return string.IsNullOrWhiteSpace(version) ? url : url + "#" + version;
        }
    }
}
