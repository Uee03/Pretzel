namespace Pretzel.Data
{
    /// <summary>How a <see cref="DataStore"/> writes its files. See <see cref="DataFormats"/> for the default extensions.</summary>
    public enum DataFormat
    {
        /// <summary>Readable JSON.</summary>
        PlainText = 0,

        /// <summary>Versioned BinaryWriter/BinaryReader encoding of JSON values.</summary>
        Binary = 1,

        /// <summary>JSON encrypted with authenticated AES-256.</summary>
        Encrypted = 2
    }
}
