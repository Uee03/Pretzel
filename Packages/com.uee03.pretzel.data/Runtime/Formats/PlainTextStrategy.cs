namespace Pretzel.Data
{
    /// <summary>Stores the serializer's UTF-8 JSON unchanged.</summary>
    public sealed class PlainTextStrategy : IDataFormatStrategy
    {
        public string DefaultExtension => "json";

        public byte[] Encode(byte[] data) => data;

        public byte[] Decode(byte[] data) => data;
    }
}
