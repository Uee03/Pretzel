using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Pretzel.Setup.Addons.Services
{
    /// <summary>
    /// Edits Packages/manifest.json. Only used for OpenUPM packages, which need a scoped registry.
    /// Everything else goes through the Package Manager API.
    /// </summary>
    public static class ManifestEditor
    {
        public const string OpenUpmName = "OpenUPM";
        public const string OpenUpmUrl = "https://package.openupm.com";

        /// <summary>Adds the OpenUPM registry, the package scope and the dependency. Returns true when the file changed.</summary>
        public static bool AddOpenUpmDependency(string manifestPath, string packageName, string version)
        {
            JObject root = JObject.Parse(File.ReadAllText(manifestPath));
            bool changed = false;

            if (root["scopedRegistries"] is not JArray registries)
            {
                registries = new JArray();
                root["scopedRegistries"] = registries;
                changed = true;
            }

            JObject openUpm = registries.OfType<JObject>().FirstOrDefault(r => string.Equals((string)r["url"], OpenUpmUrl, StringComparison.OrdinalIgnoreCase));
            if (openUpm == null)
            {
                openUpm = new JObject { ["name"] = OpenUpmName, ["url"] = OpenUpmUrl, ["scopes"] = new JArray() };
                registries.Add(openUpm);
                changed = true;
            }

            if (openUpm["scopes"] is not JArray scopes)
            {
                scopes = new JArray();
                openUpm["scopes"] = scopes;
                changed = true;
            }

            if (!scopes.Any(s => (string)s == packageName))
            {
                scopes.Add(packageName);
                changed = true;
            }

            if (root["dependencies"] is not JObject dependencies)
            {
                dependencies = new JObject();
                root["dependencies"] = dependencies;
                changed = true;
            }

            if ((string)dependencies[packageName] != version)
            {
                dependencies[packageName] = version;
                changed = true;
            }

            if (changed)
            {
                File.WriteAllText(manifestPath, root.ToString(Formatting.Indented) + "\n");
            }

            return changed;
        }
    }
}
