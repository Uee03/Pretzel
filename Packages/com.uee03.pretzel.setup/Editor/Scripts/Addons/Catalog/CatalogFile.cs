using System;
using System.Collections.Generic;

namespace Pretzel.Setup.Addons.Catalog
{
    [Serializable]
    public sealed class CatalogFile
    {
        public int schemaVersion = 1;
        public List<CatalogEntry> entries = new();
    }
}
