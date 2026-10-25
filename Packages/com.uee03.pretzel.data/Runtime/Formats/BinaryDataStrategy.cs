namespace Pretzel.Data
{
    /// <summary>Stores binary values using BinaryWriter/BinaryReader, while preserving serializer and schema support.</summary>
    public sealed class BinaryDataStrategy : IDataFormatStrategy
    {
        private readonly BinaryTransform _transform = new();

        public string DefaultExtension => "bin";

        public byte[] Encode(byte[] data) => _transform.Encode(data);

        public byte[] Decode(byte[] data) => _transform.Decode(data);
    }
}
