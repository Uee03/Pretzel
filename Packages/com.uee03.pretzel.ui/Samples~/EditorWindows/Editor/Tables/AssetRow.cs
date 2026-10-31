namespace Pretzel.Samples.UI.EditorWindows
{
    /// <summary>The data model for one row of the table: plain data, no UI types, so it can be sorted and filtered in ordinary C#.</summary>
    public sealed class AssetRow
    {
        public AssetRow(string path, string name, string typeName, long sizeBytes)
        {
            Path = path;
            Name = name;
            TypeName = typeName;
            SizeBytes = sizeBytes;
        }

        public string Path { get; }

        public string Name { get; }

        public string TypeName { get; }

        public long SizeBytes { get; }
    }
}
