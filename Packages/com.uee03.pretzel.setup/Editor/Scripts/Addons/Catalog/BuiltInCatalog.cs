using System.Collections.Generic;

namespace Pretzel.Setup.Addons.Catalog
{
    /// <summary>
    /// The launch catalog. Sources and versions were checked against each project's README on 2026-10-18.
    /// Versions are pinned, and a project can override them in the Pretzel settings.
    /// </summary>
    public static class BuiltInCatalog
    {
        public const string NuGetForUnityId = "nugetforunity";

        public static List<CatalogEntry> Create()
        {
            return new List<CatalogEntry>
            {
                new()
                {
                    id = NuGetForUnityId,
                    packageName = "com.github-glitchenzo.nugetforunity",
                    displayName = "NuGetForUnity",
                    description = "A NuGet client inside the Editor. Needed for packages that ship on NuGet, such as R3 and ZLinq.",
                    source = CatalogSource.GitUrl,
                    url = "https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity",
                    version = "v4.5.0",
                    homepage = "https://github.com/GlitchEnzo/NuGetForUnity"
                },
                new()
                {
                    id = "newtonsoft-json",
                    packageName = "com.unity.nuget.newtonsoft-json",
                    displayName = "Newtonsoft.Json",
                    description = "The default serializer of Pretzel Data. Already installed with it.",
                    source = CatalogSource.UnityRegistry,
                    version = "3.2.1",
                    homepage = "https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.2/manual/index.html"
                },
                new()
                {
                    id = "unitask",
                    packageName = "com.cysharp.unitask",
                    displayName = "UniTask",
                    description = "Allocation-free async and await for Unity, with PlayerLoop timing and async LINQ.",
                    source = CatalogSource.GitUrl,
                    url = "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
                    version = "2.5.11",
                    homepage = "https://github.com/Cysharp/UniTask"
                },
                new()
                {
                    id = "litmotion",
                    packageName = "com.annulusgames.lit-motion",
                    displayName = "LitMotion",
                    description = "A fast, zero-allocation tween library. Pretzel.Tween maps its ease types to LitMotion.",
                    source = CatalogSource.GitUrl,
                    url = "https://github.com/annulusgames/LitMotion.git?path=src/LitMotion/Assets/LitMotion",
                    version = "v2.0.2",
                    registryNote = "Needs Burst, Collections and Mathematics. The Package Manager installs them as dependencies.",
                    homepage = "https://github.com/annulusgames/LitMotion"
                },
                new()
                {
                    id = "r3",
                    packageName = "com.cysharp.r3",
                    displayName = "R3",
                    description = "Reactive Extensions for Unity, built on a modern core with frame-based operators.",
                    source = CatalogSource.GitUrl,
                    url = "https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity",
                    version = "1.3.1",
                    dependencies = new List<string> { NuGetForUnityId },
                    nugetPackage = "R3",
                    nugetVersion = "1.3.1",
                    registryNote = "Install the R3 NuGet package first, with NuGetForUnity: NuGet > Manage NuGet Packages, search R3. If Unity reports version conflicts, turn off Assembly Version Validation in Player Settings.",
                    homepage = "https://github.com/Cysharp/R3"
                },
                new()
                {
                    id = "zlinq",
                    packageName = "com.cysharp.zlinq",
                    displayName = "ZLinq",
                    description = "Zero-allocation LINQ, with LINQ to GameObject and LINQ to SIMD.",
                    source = CatalogSource.GitUrl,
                    url = "https://github.com/Cysharp/ZLinq.git?path=src/ZLinq.Unity/Assets/ZLinq.Unity",
                    version = "1.5.6",
                    dependencies = new List<string> { NuGetForUnityId },
                    nugetPackage = "ZLinq",
                    nugetVersion = "1.5.6",
                    registryNote = "Install the ZLinq NuGet package first, with NuGetForUnity: NuGet > Manage NuGet Packages, search ZLinq.",
                    homepage = "https://github.com/Cysharp/ZLinq"
                },
                new()
                {
                    id = "vcontainer",
                    packageName = "jp.hadashikick.vcontainer",
                    displayName = "VContainer",
                    description = "A fast dependency injection container for Unity, with its own PlayerLoop entry points.",
                    source = CatalogSource.GitUrl,
                    url = "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer",
                    version = "1.19.0",
                    homepage = "https://github.com/hadashiA/VContainer"
                }
            };
        }
    }
}
