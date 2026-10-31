using System;
using Pretzel.Core;
using UnityEngine;

namespace Pretzel.Multiplayer
{
    /// <summary>Stores the record as JSON in PlayerPrefs.</summary>
    public sealed class PlayerPrefsReconnectStore : IReconnectStore
    {
        public const string DefaultKey = "pretzel.multiplayer.reconnect";

        private readonly string _key;

        public PlayerPrefsReconnectStore(string key = DefaultKey)
        {
            _key = Guard.NotNullOrEmpty(key, nameof(key));
        }

        public ReconnectRecord Load()
        {
            string json = PlayerPrefs.GetString(_key, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<ReconnectRecord>(json);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public void Save(ReconnectRecord record)
        {
            Guard.NotNull(record, nameof(record));
            PlayerPrefs.SetString(_key, JsonUtility.ToJson(record));
            PlayerPrefs.Save();
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(_key);
            PlayerPrefs.Save();
        }
    }
}
