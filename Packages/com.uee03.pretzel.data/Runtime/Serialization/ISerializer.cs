namespace Pretzel.Data
{
    /// <summary>Turns objects into text and back. Implementations: <see cref="NewtonsoftSerializer"/>, <see cref="JsonUtilitySerializer"/>.</summary>
    public interface ISerializer
    {
        string Serialize<T>(T value, bool prettyPrint);

        T Deserialize<T>(string text);

        /// <summary>Writes the serialized values into an existing object. Use this for ScriptableObjects.</summary>
        void Populate<T>(string text, T target);
    }
}
