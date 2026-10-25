using System;

namespace Pretzel.Data
{
    public static class DataFormats
    {
        /// <summary>Extension without the dot: json, bin, pretzel.</summary>
        public static string DefaultExtension(DataFormat format)
        {
            return format switch
            {
                DataFormat.PlainText => "json",
                DataFormat.Binary => "bin",
                DataFormat.Encrypted => "pretzel",
                _ => throw new NotSupportedException($"Data format {format} is not supported.")
            };
        }
    }
}
