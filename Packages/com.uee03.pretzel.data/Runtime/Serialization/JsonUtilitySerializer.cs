using UnityEngine;

namespace Pretzel.Data
{
    /// <summary>
    /// Unity's <see cref="JsonUtility"/>. Use it for ScriptableObjects and for projects without Newtonsoft.
    /// The root must be a serializable class or struct. Arrays, lists and dictionaries need a wrapper type.
    /// </summary>
    public sealed class JsonUtilitySerializer : ISerializer
    {
        public string Serialize<T>(T value, bool prettyPrint)
        {
            return JsonUtility.ToJson(value, prettyPrint);
        }

        public T Deserialize<T>(string text)
        {
            return JsonUtility.FromJson<T>(text);
        }

        public void Populate<T>(string text, T target)
        {
            JsonUtility.FromJsonOverwrite(text, target);
        }
    }
}
