namespace Pretzel.Data
{
    /// <summary>A reversible file encoding, independent of the serializer and storage location.</summary>
    public interface IDataFormatStrategy : IDataTransform
    {
        string DefaultExtension { get; }
    }
}
