using UnityEngine;

namespace Pretzel.Inputs
{
    /// <summary>Keeps binding overrides in PlayerPrefs. Use <c>PreferencesBindingStore</c> instead when Pretzel Preferences is installed.</summary>
    public sealed class PlayerPrefsBindingStore : IBindingStore
    {
        public const string DefaultKey = "pretzel.input.bindings";

        private readonly string _key;

        public PlayerPrefsBindingStore(string key = DefaultKey)
        {
            _key = string.IsNullOrEmpty(key) ? DefaultKey : key;
        }

        public string Load()
        {
            return PlayerPrefs.GetString(_key, string.Empty);
        }

        public void Save(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                PlayerPrefs.DeleteKey(_key);
            }
            else
            {
                PlayerPrefs.SetString(_key, json);
            }

            PlayerPrefs.Save();
        }
    }
}
