using System.Collections.Generic;
using Pretzel.Setup.Settings;
using UnityEditor.PackageManager;

namespace Pretzel.Setup.Addons.Catalog
{
    /// <summary>One Pretzel module. Mirrors the dependencies in each module's package.json.</summary>
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

    public static class ModuleCatalog
    {
        private const string Prefix = "com.uee03.pretzel.";
        private const string SetupPackage = Prefix + "setup";

        /// <summary>The repository and tag modules install from: the Settings values, else the git source this package was installed from.</summary>
        public static (string Url, string Version) ResolveSource(PretzelProjectSettings settings)
        {
            if (!string.IsNullOrWhiteSpace(settings.moduleSourceUrl))
            {
                return (settings.moduleSourceUrl.Trim(), (settings.moduleSourceVersion ?? string.Empty).Trim());
            }

            return InferSource();
        }

        // A git package id looks like name@url?path=sub#revision.
        private static (string Url, string Version) InferSource()
        {
            PackageInfo info = PackageInfo.FindForAssembly(typeof(ModuleCatalog).Assembly);
            if (info == null || info.source != PackageSource.Git)
            {
                return (null, null);
            }

            int at = info.packageId.IndexOf('@');
            if (at < 0)
            {
                return (null, null);
            }

            string rest = info.packageId.Substring(at + 1);
            string version = string.Empty;
            int hash = rest.IndexOf('#');
            if (hash >= 0)
            {
                version = rest.Substring(hash + 1);
                rest = rest.Substring(0, hash);
            }

            int query = rest.IndexOf('?');
            return (query >= 0 ? rest.Substring(0, query) : rest, version);
        }

        public static IReadOnlyList<ModuleInfo> All { get; } = new List<ModuleInfo>
        {
            new(Prefix + "core", "Core", "Guard checks and singletons shared by every module.", new string[0]),
            new(Prefix + "extensions", "Extensions", "Extension methods for strings, collections, GameObjects, Transforms and files.", new[] { Prefix + "core" }),
            new(Prefix + "data", "Data", "Save and load with keys, slots, atomic writes, migrations and a pluggable serializer. Includes the Data Format window and the SerializableGuid drawer.", new[] { Prefix + "core", Prefix + "extensions" }, null, new[] { "com.unity.nuget.newtonsoft-json" }),
            new(Prefix + "tween", "Tween", "Easing functions for float, Vector2, Vector3 and Color.", new[] { Prefix + "core" }),
            new(Prefix + "ui", "UI (UI Toolkit)", "Element factories, USS-transition fades, a hover fade and a screen controller base.", new[] { Prefix + "core", Prefix + "tween" }),
            new(Prefix + "ui.ugui", "UI (UGUI)", "UIFader, hover fade and Button extensions for Unity UI.", new[] { Prefix + "core", Prefix + "ui", Prefix + "tween" }),
            new(Prefix + "api", "API", "An Awaitable HTTP client with retries, timeouts and cancellation.", new[] { Prefix + "core", Prefix + "data" }),
            new(Prefix + "templates", "Templates", "Script templates for Pretzel types: MonoBehaviour, UI controller and singleton.", new[] { Prefix + "core", Prefix + "ui" }),
            new(Prefix + "integrations", "Integrations", "Adapters between Pretzel and LitMotion, UniTask and R3.", new[] { Prefix + "core", Prefix + "tween", Prefix + "api", Prefix + "ui" },
                new[] { ("LitMotion", "com.annulusgames.lit-motion"), ("UniTask", "com.cysharp.unitask"), ("R3", "com.cysharp.r3") }),
            new(Prefix + "editor", "Editor", "Standalone editor utilities that work in any project, such as grouping selected objects.", new string[0])
        };
    }
}
