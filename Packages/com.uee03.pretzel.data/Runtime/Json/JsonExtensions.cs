namespace Pretzel.Data
{
    /// <summary>
    /// String helpers that use a shared <see cref="NewtonsoftSerializer"/>, or the serializer you pass in.
    /// They never build the <see cref="DataIO"/> store, so they work before keys exist.
    /// </summary>
    public static class JsonExtensions
    {
        private static readonly ISerializer DefaultSerializer = new NewtonsoftSerializer();

        public static string ToJson<T>(this T value, bool prettyPrint = false, ISerializer serializer = null)
        {
            return (serializer ?? DefaultSerializer).Serialize(value, prettyPrint);
        }

        public static T FromJson<T>(this string json, ISerializer serializer = null)
        {
            return (serializer ?? DefaultSerializer).Deserialize<T>(json);
        }

        public static void FromJsonOverwrite<T>(this string json, T target, ISerializer serializer = null) where T : class
        {
            (serializer ?? DefaultSerializer).Populate(json, target);
        }
    }
}
