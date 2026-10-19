using System.IO;
using System.IO.Compression;

namespace Pretzel.Data
{
    public sealed class GZipTransform : IDataTransform
    {
        public byte[] Encode(byte[] data)
        {
            using MemoryStream output = new();
            using (GZipStream gzip = new(output, CompressionLevel.Optimal, true))
            {
                gzip.Write(data, 0, data.Length);
            }

            return output.ToArray();
        }

        public byte[] Decode(byte[] data)
        {
            using MemoryStream input = new(data);
            using GZipStream gzip = new(input, CompressionMode.Decompress);
            using MemoryStream output = new();
            gzip.CopyTo(output);
            return output.ToArray();
        }
    }
}
