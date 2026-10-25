using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Pretzel.Setup.Addons.Catalog
{
    /// <summary>One installable third-party package. The same shape is used by remote catalog files.</summary>
    [Serializable]
    public sealed class CatalogEntry
    {
        /// <summary>Stable key used by dependencies and by pinned versions.</summary>
        public string id;

        /// <summary>The UPM package name, used to detect whether it is installed.</summary>
        public string packageName;

        public string displayName;
        public string description;

        [JsonConverter(typeof(StringEnumConverter))]
        public CatalogSource source;

        /// <summary>Git URL without the tag. Only for <see cref="CatalogSource.GitUrl"/>.</summary>
        public string url;

        /// <summary>The pinned version: a registry version, or a git tag.</summary>
        public string version;

        /// <summary>Ids of catalog entries that are installed together with this one.</summary>
        public List<string> dependencies = new();

        /// <summary>A NuGet package that must be installed through NuGetForUnity as well. It cannot be installed automatically.</summary>
        public string nugetPackage;

        public string nugetVersion;

        public string registryNote;
        public string homepage;

        /// <summary>The identifier passed to the Package Manager.</summary>
        public string Identifier
        {
            get
            {
                if (source == CatalogSource.GitUrl)
                {
                    return string.IsNullOrEmpty(version) ? url : url + "#" + version;
                }

                return string.IsNullOrEmpty(version) ? packageName : packageName + "@" + version;
            }
        }

        public bool NeedsNuGet => !string.IsNullOrEmpty(nugetPackage);

        public CatalogEntry Clone()
        {
            CatalogEntry copy = (CatalogEntry)MemberwiseClone();
            copy.dependencies = new List<string>(dependencies ?? new List<string>());
            return copy;
        }
    }

}
