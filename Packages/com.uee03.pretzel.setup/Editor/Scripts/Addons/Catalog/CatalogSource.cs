namespace Pretzel.Setup.Addons.Catalog
{
    public enum CatalogSource
    {
        /// <summary>A package on the Unity registry: installed as name@version.</summary>
        UnityRegistry,

        /// <summary>A git URL, optionally pinned with a tag: installed as url#version.</summary>
        GitUrl,

        /// <summary>A package on OpenUPM. Needs a scoped registry in the manifest, which is added for you.</summary>
        OpenUpm
    }
}
