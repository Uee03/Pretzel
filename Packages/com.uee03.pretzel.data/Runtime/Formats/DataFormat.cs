namespace Pretzel.Data
{
    /// <summary>How a <see cref="DataStore"/> writes its files. See <see cref="DataFormats"/> for the default extensions.</summary>
    public enum DataFormat
    {
        /// <summary>Readable JSON.</summary>
        PlainText,

        /// <summary>JSON compressed with GZip.</summary>
        Compressed,

        /// <summary>BSON, a binary encoding of the JSON data model.</summary>
        Binary,

        /// <summary>JSON encrypted with AES-256, optionally with the AES key wrapped by an RSA public key.</summary>
        Encrypted
    }
}
