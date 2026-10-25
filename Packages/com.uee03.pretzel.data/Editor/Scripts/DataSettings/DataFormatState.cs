using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Data.Editor
{
    /// <summary>What every tab of the Data Format window shares: the config asset and the working keys.</summary>
    internal sealed class DataFormatState
    {
        private const long SaveDelayMs = 500;
        private const string PendingKey = "Pretzel.DataFormat.PendingKeys";

        private VisualElement _host;
        private IVisualElementScheduledItem _timer;

        public DataIOConfig Config { get; private set; }

        /// <summary>Keys held in memory. They are only on disk once the Keys tab stores them.</summary>
        public EncryptionKeys Keys { get; private set; }

        /// <summary>True while generated keys exist only in memory.</summary>
        public bool KeysUnstored { get; private set; }

        public string KeyLoadError { get; private set; }

        /// <summary>Raised after the config or the keys changed, so tabs can refresh.</summary>
        public event Action Changed;

        [Serializable]
        private sealed class Pending
        {
            public string aes;
            public string[] previous;
        }

        public void Initialize(VisualElement host)
        {
            _host = host;
            Config = DataIOConfigAsset.LoadOrCreate();

            // Generated keys that were not stored yet survive a domain reload, so a script recompile does not lose them.
            string pending = SessionState.GetString(PendingKey, string.Empty);
            try
            {
                if (pending.Length > 0)
                {
                    Keys = RestorePending(pending);
                    KeysUnstored = true;
                    return;
                }

                Keys = EncryptionKeys.Resolve(Config.environmentPrefix) ?? new EncryptionKeys();
                KeysUnstored = false;
            }
            catch (Exception ex) when (PanelUi.IsExpectedFailure(ex))
            {
                // Unreadable pending keys would fail again on every open.
                SessionState.EraseString(PendingKey);
                Keys = new EncryptionKeys();
                KeyLoadError = "Stored keys were rejected: " + ex.Message;
            }
        }

        internal static EncryptionKeys RestorePending(string json)
        {
            Pending saved = JsonUtility.FromJson<Pending>(json);
            EncryptionKeys keys = new() { AesKey = string.IsNullOrEmpty(saved.aes) ? null : EncryptionKeys.DecodeAesKey(saved.aes) };
            if (!keys.HasAesKey)
            {
                throw new FormatException("Pending keys must contain a 32 byte AES key.");
            }

            if (saved.previous != null)
            {
                foreach (string old in saved.previous)
                {
                    byte[] key = string.IsNullOrEmpty(old) ? null : EncryptionKeys.DecodeAesKey(old);
                    if (key == null || key.Length != EncryptionKeys.AesKeySize)
                    {
                        throw new FormatException("A pending previous AES key must contain 32 bytes.");
                    }

                    keys.AddPreviousAesKey(key);
                }
            }

            return keys;
        }

        public void Edit(string undoName, Action<DataIOConfig> change)
        {
            Undo.RecordObject(Config, undoName);
            change(Config);
            EditorUtility.SetDirty(Config);
            _timer?.Pause();
            _timer = _host.schedule.Execute(Flush).StartingIn(SaveDelayMs);
            Changed?.Invoke();
        }

        public void SetKeys(EncryptionKeys keys, bool unstored)
        {
            Keys = keys;
            KeyLoadError = null;
            KeysUnstored = unstored;
            if (unstored)
            {
                SessionState.SetString(PendingKey, JsonUtility.ToJson(new Pending
                {
                    aes = keys.HasAesKey ? EncryptionKeys.EncodeAesKey(keys.AesKey) : null,
                    previous = keys.PreviousAesKeys.ConvertAll(EncryptionKeys.EncodeAesKey).ToArray()
                }));
            }
            else
            {
                SessionState.EraseString(PendingKey);
            }

            Changed?.Invoke();
        }

        public void MarkKeysStored()
        {
            KeysUnstored = false;
            SessionState.EraseString(PendingKey);
            Changed?.Invoke();
        }

        /// <summary>Re-reads the keys from their stored places, for example after the prefix changed.</summary>
        public void ReloadKeys()
        {
            try
            {
                SetKeys(EncryptionKeys.Resolve(Config.environmentPrefix) ?? new EncryptionKeys(), false);
            }
            catch (Exception ex) when (PanelUi.IsExpectedFailure(ex))
            {
                KeyLoadError = "Stored keys were rejected: " + ex.Message;
                Changed?.Invoke();
            }
        }

        public void DiscardUnstoredKeys()
        {
            SessionState.EraseString(PendingKey);
            KeysUnstored = false;
        }

        /// <summary>Writes the keys of the current mode to the project key file.</summary>
        public void SaveKeysToFile()
        {
            KeyStorage.SaveFile(KeyStorage.ForMode(Keys, Config.encryptionMode), KeyScope.All);
            MarkKeysStored();
        }

        public void Flush()
        {
            _timer?.Pause();
            _timer = null;
            if (Config)
            {
                AssetDatabase.SaveAssetIfDirty(Config);
            }
        }
    }
}
