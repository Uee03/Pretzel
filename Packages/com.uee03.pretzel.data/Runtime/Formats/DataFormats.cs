namespace Pretzel.Data
{
    public static class DataFormats
    {
        /// <summary>Extension without the dot: json, json.gz, bin, pretzel.</summary>
        public static string DefaultExtension(DataFormat format)
        {
            return format switch
            {
                DataFormat.Compressed => "json.gz",
                DataFormat.Binary => "bin",
                DataFormat.Encrypted => "pretzel",
                _ => "json"
            };
        }
    }
}
