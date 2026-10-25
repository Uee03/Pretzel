namespace Pretzel.Data
{
    /// <summary>A reversible byte-level step applied between the serializer and the disk, for example binary encoding or encryption.</summary>
    public interface IDataTransform
    {
        /// <summary>Applied on save. Transforms run in list order.</summary>
        byte[] Encode(byte[] data);

        /// <summary>Applied on load. Transforms run in reverse list order.</summary>
        byte[] Decode(byte[] data);
    }
}
