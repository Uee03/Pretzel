using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pretzel.Setup.Addons.Catalog;
using UnityEditor.PackageManager;

namespace Pretzel.Setup.Tests
{
    public sealed class ModuleCatalogTests
    {
        private const string Prefix = "com.uee03.pretzel.";

        [Test]
        public void CatalogLoadsEveryModuleExceptSetup()
        {
            IReadOnlyList<ModuleInfo> modules = ModuleCatalog.All;
            Assert.That(modules, Is.Not.Empty);
            Assert.That(modules.Select(m => m.Name), Does.Contain(Prefix + "core"));
            Assert.That(modules.Select(m => m.Name), Does.Not.Contain(Prefix + "setup"));
            Assert.That(modules.Select(m => m.Name), Is.Unique);
            foreach (ModuleInfo module in modules)
            {
                Assert.That(module.DisplayName, Is.Not.Empty, module.Name);
                Assert.That(module.Description, Is.Not.Empty, module.Name);
            }
        }

        [Test]
        public void CatalogDependenciesPointToCatalogModules()
        {
            HashSet<string> names = new(ModuleCatalog.All.Select(m => m.Name));
            foreach (ModuleInfo module in ModuleCatalog.All)
            {
                foreach (string dependency in module.Dependencies)
                {
                    Assert.That(names, Does.Contain(dependency), $"{module.Name} depends on an unknown module.");
                }
            }
        }

        // Fails when a package.json changed without running .github/scripts/Update-ModuleCatalog.ps1.
        [Test]
        public void CatalogMatchesInstalledManifests()
        {
            Dictionary<string, PackageInfo> installed = PackageInfo.GetAllRegisteredPackages()
                .Where(p => p.name.StartsWith(Prefix))
                .ToDictionary(p => p.name);

            foreach (ModuleInfo module in ModuleCatalog.All)
            {
                if (!installed.TryGetValue(module.Name, out PackageInfo package))
                {
                    continue;
                }

                string[] manifestDependencies = package.dependencies.Select(d => d.name).Where(n => n.StartsWith(Prefix)).OrderBy(n => n).ToArray();
                Assert.That(module.Dependencies.OrderBy(n => n).ToArray(), Is.EqualTo(manifestDependencies), module.Name);
            }
        }

        [Test]
        public void ParseReadsOptionalLibraries()
        {
            const string json = "{ \"modules\": [ { \"name\": \"com.uee03.pretzel.x\", \"displayName\": \"X\", \"description\": \"d\", " +
                                "\"dependencies\": [\"com.uee03.pretzel.core\"], \"packages\": [\"com.unity.ugui\"], " +
                                "\"optionalLibraries\": [ { \"displayName\": \"R3\", \"packageName\": \"com.cysharp.r3\" } ] } ] }";
            ModuleInfo module = ModuleCatalog.Parse(json).Single();
            Assert.That(module.Dependencies, Is.EqualTo(new[] { "com.uee03.pretzel.core" }));
            Assert.That(module.Packages, Is.EqualTo(new[] { "com.unity.ugui" }));
            Assert.That(module.OptionalLibraries.Single(), Is.EqualTo(("R3", "com.cysharp.r3")));
        }
    }
}
