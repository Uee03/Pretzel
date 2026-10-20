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

        /// <summary>Raised after the config or the keys changed, so tabs can refresh.</summary>
        public event Action Changed;

        [Serializable]
        private sealed class Pending
        {
            public string aes;
            public string publicKey;
            public string privateKey;
            public string[] previous;
        }

        public void Initialize(VisualElement host)
        {
            _host = host;
            Config = DataIOConfigAsset.LoadOrCreate();

            // Generated keys that were not stored yet survive a domain reload, so a script recompile does not lose them.
            string pending = SessionState.GetString(PendingKey, string.Empty);
            if (pending.Length > 0)
            {
                Pending saved = JsonUtility.FromJson<Pending>(pending);
                Keys = new EncryptionKeys
                {
                    AesKey = string.IsNullOrEmpty(saved.aes) ? null : EncryptionKeys.DecodeAesKey(saved.aes),
                    RsaPublicKey = EncryptionKeys.DecodeXml(saved.publicKey),
                    RsaPrivateKey = EncryptionKeys.DecodeXml(saved.privateKey)
                };
                if (saved.previous != null)
                {
                    foreach (string old in saved.previous)
                    {
                        Keys.AddPreviousAesKey(string.IsNullOrEmpty(old) ? null : EncryptionKeys.DecodeAesKey(old));
                    }
                }

                KeysUnstored = true;
                return;
            }

            Keys = EncryptionKeys.Resolve(Config.environmentPrefix) ?? new EncryptionKeys();
            KeysUnstored = false;
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
            KeysUnstored = unstored;
            if (unstored)
            {
                SessionState.SetString(PendingKey, JsonUtility.ToJson(new Pending
                {
                    aes = keys.HasAesKey ? EncryptionKeys.EncodeAesKey(keys.AesKey) : null,
                    publicKey = EncryptionKeys.EncodeXml(keys.RsaPublicKey),
                    privateKey = EncryptionKeys.EncodeXml(keys.RsaPrivateKey),
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
            SetKeys(EncryptionKeys.Resolve(Config.environmentPrefix) ?? new EncryptionKeys(), false);
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
