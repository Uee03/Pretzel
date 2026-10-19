using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Data.Editor
{
    internal sealed class KeysPanel : IDataFormatPanel
    {
        private const string GameConfigPath = "Assets/Resources/" + GameEncryptionKeyConfig.ResourcePath + ".json";

        private DataFormatState _state;
        private TextField _prefix;
        private Label _prefixHint;
        private Label _result;
        private Label _envHint;
        private Label _filePath;
        private Label _git;
        private VisualElement _publicSection;
        private Button _envStorePublic;
        private Button _fileStorePublic;
        private Button _envStore;
        private Button _fileStore;
        private Button _envCopy;
        private Button _fileDelete;
        private Button _fileReveal;
        private Button _gameJson;
        private Button _gameEmbedded;
        private Button _gameSelect;
        private Label _gameHint;
        private Label _gameResult;

        private KeyRow _aesRow;
        private KeyRow _publicRow;
        private KeyRow _privateRow;

        private sealed class KeyRow
        {
            public VisualElement Root;
            public Label Memory;
            public Label File;
            public Label Environment;
        }

        public string Title => "Keys";

        public VisualElement CreateView(DataFormatState state)
        {
            _state = state;
            VisualElement view = DataEditorAssets.Layout("KeysTab").Instantiate();
            view.style.flexGrow = 1f;

            _prefix = view.Q<TextField>("keys-panel__prefix");
            _prefixHint = view.Q<Label>("keys-panel__prefix-hint");
            _result = view.Q<Label>("keys-panel__result");
            _envHint = view.Q<Label>("keys-panel__env-hint");
            _filePath = view.Q<Label>("keys-panel__file-path");
            _git = view.Q<Label>("keys-panel__git");
            _publicSection = view.Q("keys-panel__public-section");
            _envStore = view.Q<Button>("keys-panel__env-store");
            _envStorePublic = view.Q<Button>("keys-panel__env-store-public");
            _envCopy = view.Q<Button>("keys-panel__env-copy");
            _fileStore = view.Q<Button>("keys-panel__file-store");
            _fileStorePublic = view.Q<Button>("keys-panel__file-store-public");
            _fileReveal = view.Q<Button>("keys-panel__file-reveal");
            _fileDelete = view.Q<Button>("keys-panel__file-delete");
            _gameJson = view.Q<Button>("keys-panel__game-json");
            _gameEmbedded = view.Q<Button>("keys-panel__game-embedded");
            _gameSelect = view.Q<Button>("keys-panel__game-select");
            _gameHint = view.Q<Label>("keys-panel__game-hint");
            _gameResult = view.Q<Label>("keys-panel__game-result");

            VisualElement rows = view.Q("keys-panel__rows");
            _aesRow = AddRow(rows, "AES key");
            _publicRow = AddRow(rows, "RSA public key");
            _privateRow = AddRow(rows, "RSA private key");

            _envHint.text = KeyStorage.CanPersistEnvironment
                ? "Saved to your Windows user environment, so every program started afterwards sees them. Restart Unity Hub and Unity if they are not picked up. This Editor session gets them immediately."
                : "This platform cannot save user variables from the Editor. They are set for this Editor session only. Use Copy commands and add them to your shell profile.";
            _filePath.text = EncryptionKeyFile.Path;

            _prefix.RegisterValueChangedCallback(evt => OnPrefixChanged(evt.newValue.Trim()));
            view.Q<Button>("keys-panel__reload").clicked += OnReload;
            _envStore.clicked += () => StoreEnvironment(KeyScope.All);
            _envStorePublic.clicked += () => StoreEnvironment(KeyScope.PublicOnly);
            _envCopy.clicked += OnCopyCommands;
            view.Q<Button>("keys-panel__env-remove").clicked += OnRemoveEnvironment;
            _fileStore.clicked += () => StoreFile(KeyScope.All);
            _fileStorePublic.clicked += () => StoreFile(KeyScope.PublicOnly);
            _fileReveal.clicked += () => EditorUtility.RevealInFinder(File.Exists(EncryptionKeyFile.Path) ? EncryptionKeyFile.Path : Path.GetDirectoryName(EncryptionKeyFile.Path));
            _fileDelete.clicked += OnDeleteFile;
            _gameJson.clicked += () => ExportGameKeys(false);
            _gameEmbedded.clicked += () => ExportGameKeys(true);
            _gameSelect.clicked += () => SelectAsset(GameConfigPath);

            _state.Changed += Refresh;
            Refresh();
            return view;
        }

        public void OnSelected() => Refresh();

        private static KeyRow AddRow(VisualElement parent, string name)
        {
            KeyRow row = new() { Root = new VisualElement(), Memory = new Label(), File = new Label(), Environment = new Label() };
            row.Root.AddToClassList("pretzel-key-row");
            Label label = new(name);
            label.AddToClassList("pretzel-key-row__name");
            row.Root.Add(label);
            foreach (Label chip in new[] { row.Memory, row.File, row.Environment })
            {
                chip.AddToClassList("pretzel-key-row__chip");
                row.Root.Add(chip);
            }

            parent.Add(row.Root);
            return row;
        }

        private void Refresh()
        {
            DataIOConfig config = _state.Config;
            if (!config)
            {
                return;
            }

            string prefix = config.environmentPrefix;
            _prefix.SetValueWithoutNotify(prefix);
            _prefixHint.text = $"Variables: {EncryptionKeys.AesVariable(prefix)}, {EncryptionKeys.PublicVariable(prefix)}, {EncryptionKeys.PrivateVariable(prefix)}";

            bool rsa = config.encryptionMode == EncryptionMode.Rsa;
            PanelUi.SetVisible(_aesRow.Root, !rsa);
            PanelUi.SetVisible(_publicRow.Root, rsa);
            PanelUi.SetVisible(_privateRow.Root, rsa);

            EncryptionKeys keys = _state.Keys;
            EncryptionKeys onFile = EncryptionKeyFile.Load() ?? new EncryptionKeys();
            ShowRow(_aesRow, keys.HasAesKey, onFile.HasAesKey, EncryptionKeys.AesVariable(prefix));
            ShowRow(_publicRow, keys.HasPublicKey, onFile.HasPublicKey, EncryptionKeys.PublicVariable(prefix));
            ShowRow(_privateRow, keys.HasPrivateKey, onFile.HasPrivateKey, EncryptionKeys.PrivateVariable(prefix));

            bool hasKeys = rsa ? keys.HasPublicKey : keys.HasAesKey;
            _envStore.SetEnabled(hasKeys);
            _fileStore.SetEnabled(hasKeys);
            _envCopy.SetEnabled(hasKeys);
            _gameJson.SetEnabled(hasKeys);
            _gameEmbedded.SetEnabled(hasKeys);
            _gameSelect.SetEnabled(File.Exists(GameConfigPath));
            _gameHint.text = $"JSON: {GameConfigPath}\nC#: {ShippingGenerator.DefaultFolder}/{ShippingGenerator.ScriptName}\n"
                             + (rsa
                                 ? "Exports only the RSA public key. The game can write encrypted files, but reading them requires the private key on your server."
                                 : "Exports the current AES key so the game can both save and load encrypted files offline.");

            // The public key section only matters when there is a public key.
            bool showPublic = keys.HasPublicKey;
            PanelUi.SetVisible(_publicSection, showPublic);
            PanelUi.SetVisible(_envStorePublic, showPublic);
            PanelUi.SetVisible(_fileStorePublic, showPublic);

            _fileDelete.SetEnabled(EncryptionKeyFile.Exists);

            switch (KeyStorage.UserSettingsIgnoreState())
            {
                case GitIgnoreState.Ignored:
                    PanelUi.SetChip(_git, "UserSettings is in .gitignore", PanelUi.Ok);
                    break;
                case GitIgnoreState.NotIgnored:
                    PanelUi.SetChip(_git, "UserSettings is NOT in .gitignore. Add it before saving keys.", PanelUi.Missing);
                    break;
                default:
                    PanelUi.SetChip(_git, "No .gitignore found. Do not commit the UserSettings folder.", PanelUi.Warn);
                    break;
            }
        }

        private static void ShowRow(KeyRow row, bool inMemory, bool inFile, string variable)
        {
            PanelUi.SetChip(row.Memory, inMemory ? "In memory" : "Not in memory", inMemory ? PanelUi.Ok : PanelUi.Warn);
            PanelUi.SetChip(row.File, inFile ? "Key file" : "No key file entry", inFile ? PanelUi.Ok : PanelUi.Warn);

            EnvironmentStatus status = KeyStorage.StatusOf(variable);
            string text = status == EnvironmentStatus.Saved ? "Environment: saved"
                : status == EnvironmentStatus.SessionOnly ? "Environment: this session"
                : "Environment: not set";
            PanelUi.SetChip(row.Environment, text, status == EnvironmentStatus.NotSet ? PanelUi.Warn : PanelUi.Ok);
        }

        private EncryptionKeys KeysForMode()
        {
            EncryptionKeys keys = _state.Keys;
            return _state.Config.encryptionMode == EncryptionMode.Aes
                ? new EncryptionKeys { AesKey = keys.AesKey }
                : new EncryptionKeys { RsaPublicKey = keys.RsaPublicKey, RsaPrivateKey = keys.RsaPrivateKey };
        }

        private void OnPrefixChanged(string value)
        {
            if (!DataIOConfigAsset.IsValidPrefix(value))
            {
                Debug.LogWarning("[Pretzel] The prefix can only contain letters, digits and underscores, and cannot start with a digit.");
                _prefix.SetValueWithoutNotify(_state.Config.environmentPrefix);
                return;
            }

            _state.Edit("Change variable prefix", c => c.environmentPrefix = value);
        }

        private void OnReload()
        {
            if (_state.KeysUnstored && !EditorUtility.DisplayDialog("Discard unsaved keys?", "The generated keys are only in memory. Reloading replaces them with what is stored.", "Discard", "Cancel"))
            {
                return;
            }

            _state.ReloadKeys();
            PanelUi.SetText(_result, "Keys reloaded from storage.");
        }

        private void StoreEnvironment(KeyScope scope)
        {
            EncryptionKeys keys = KeysForMode();
            KeyStorage.SaveEnvironment(_state.Config.environmentPrefix, keys, scope);
            if (scope == KeyScope.All)
            {
                _state.MarkKeysStored();
            }
            string suffix = KeyStorage.CanPersistEnvironment ? " Restart other programs to see them." : " They last until the Editor closes.";
            PanelUi.SetText(_result, (scope == KeyScope.PublicOnly ? "Public key" : "Keys") + " stored as environment variables." + suffix);
        }

        private void OnCopyCommands()
        {
            EncryptionKeys keys = KeysForMode();
            if (keys.HasPrivateKey && !EditorUtility.DisplayDialog("Copy the private key?", "The commands contain the private key in plain text. Paste them only into a trusted shell or secret store.", "Copy", "Cancel"))
            {
                return;
            }

            PanelUi.Copy(KeyStorage.ShellCommands(_state.Config.environmentPrefix, keys, KeyScope.All));
            PanelUi.SetText(_result, "Commands copied.");
        }

        private void OnRemoveEnvironment()
        {
            if (!EditorUtility.DisplayDialog("Remove the variables?", $"This removes the {_state.Config.environmentPrefix}_* variables from your user environment. Make sure the keys are stored somewhere else.", "Remove", "Cancel"))
            {
                return;
            }

            KeyStorage.ClearEnvironment(_state.Config.environmentPrefix);
            _state.ReloadKeys();
            PanelUi.SetText(_result, "Environment variables removed.");
        }

        private void StoreFile(KeyScope scope)
        {
            KeyStorage.SaveFile(KeysForMode(), scope);
            if (scope == KeyScope.All)
            {
                _state.MarkKeysStored();
            }
            PanelUi.SetText(_result, (scope == KeyScope.PublicOnly ? "Public key" : "Keys") + " saved to the project key file.");
        }

        private void OnDeleteFile()
        {
            if (!EditorUtility.DisplayDialog("Delete the key file?", "Keys that are only in this file are lost, and files encrypted with them can no longer be opened.", "Delete", "Cancel"))
            {
                return;
            }

            EncryptionKeyFile.Delete();
            _state.ReloadKeys();
            PanelUi.SetText(_result, "Key file deleted.");
        }

        private void ExportGameKeys(bool embedded)
        {
            string path = embedded ? ShippingGenerator.DefaultFolder + "/" + ShippingGenerator.ScriptName : GameConfigPath;
            bool aes = _state.Config.encryptionMode == EncryptionMode.Aes;
            if ((aes || File.Exists(path)) && !EditorUtility.DisplayDialog("Export game key?",
                    $"This writes {(aes ? "the AES key" : "only the RSA public key")} to {path} and includes it in builds. "
                    + "Any existing file at this path will be replaced. Changing a key does not migrate existing saves."
                    + (aes ? " Anyone with the game can recover this key. Keep a separate backup and do not commit the generated file." : ""),
                    "Export", "Cancel"))
            {
                return;
            }

            try
            {
                if (embedded)
                {
                    string error = ShippingGenerator.Generate(ShippingGenerator.DefaultFolder, KeyDelivery.Embedded,
                        _state.Config, _state.Keys, out path);
                    if (error != null)
                    {
                        PanelUi.SetText(_gameResult, error, "error");
                        return;
                    }
                }
                else
                {
                    string json = GameEncryptionKeyConfig.ToJson(_state.Keys, _state.Config.encryptionMode);
                    Directory.CreateDirectory(Path.GetDirectoryName(GameConfigPath));
                    File.WriteAllText(GameConfigPath, json, new UTF8Encoding(false));
                    AssetDatabase.ImportAsset(GameConfigPath);
                }

                // RSA export deliberately leaves the private key unstored.
                if (aes)
                {
                    _state.MarkKeysStored();
                }

                Refresh();
                PanelUi.SetText(_gameResult, $"Created {path}. Restart Play mode to use the exported key. Keep older keys for existing saves.");
                SelectAsset(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
                                       or InvalidOperationException or CryptographicException or System.Xml.XmlException)
            {
                PanelUi.SetText(_gameResult, $"Could not export game keys: {PanelUi.ErrorText(ex)}", "error");
            }
        }

        private static void SelectAsset(string path)
        {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
