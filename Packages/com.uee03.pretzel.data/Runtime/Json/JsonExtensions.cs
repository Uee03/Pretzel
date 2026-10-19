namespace Pretzel.Data
{
    /// <summary>String helpers that route through the serializer of <see cref="DataIO.Default"/>, or one you pass in.</summary>
    public static class JsonExtensions
    {
        public static string ToJson<T>(this T value, bool prettyPrint = false, ISerializer serializer = null)
        {
            return (serializer ?? DataIO.Default.Serializer).Serialize(value, prettyPrint);
        }

        public static T FromJson<T>(this string json, ISerializer serializer = null)
        {
            return (serializer ?? DataIO.Default.Serializer).Deserialize<T>(json);
        }

        public static void FromJsonOverwrite<T>(this string json, T target, ISerializer serializer = null) where T : class
        {
            (serializer ?? DataIO.Default.Serializer).Populate(json, target);
        }
    }
}
