using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Data.Editor
{
    internal sealed class KeysPanel : IDataFormatPanel
    {
        private DataFormatState _state;
        private TextField _prefix;
        private EnumField _source;
        private Label _active;
        private Label _activeDetail;
        private Button _previousClear;
        private Label _prefixHint;
        private Label _result;
        private Label _envHint;
        private Label _filePath;
        private Label _git;
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
            _source = view.Q<EnumField>("keys-panel__source");
            _active = view.Q<Label>("keys-panel__active");
            _activeDetail = view.Q<Label>("keys-panel__active-detail");
            _previousClear = view.Q<Button>("keys-panel__previous-clear");
            _prefixHint = view.Q<Label>("keys-panel__prefix-hint");
            _result = view.Q<Label>("keys-panel__result");
            _envHint = view.Q<Label>("keys-panel__env-hint");
            _filePath = view.Q<Label>("keys-panel__file-path");
            _git = view.Q<Label>("keys-panel__git");
            _envStore = view.Q<Button>("keys-panel__env-store");
            _envCopy = view.Q<Button>("keys-panel__env-copy");
            _fileStore = view.Q<Button>("keys-panel__file-store");
            _fileReveal = view.Q<Button>("keys-panel__file-reveal");
            _fileDelete = view.Q<Button>("keys-panel__file-delete");
            _gameJson = view.Q<Button>("keys-panel__game-json");
            _gameEmbedded = view.Q<Button>("keys-panel__game-embedded");
            _gameSelect = view.Q<Button>("keys-panel__game-select");
            _gameHint = view.Q<Label>("keys-panel__game-hint");
            _gameResult = view.Q<Label>("keys-panel__game-result");

            VisualElement rows = view.Q("keys-panel__rows");
            _aesRow = AddRow(rows, "AES key");

            _envHint.text = KeyStorage.CanPersistEnvironment
                ? "Saved to your Windows user environment, so every program started afterwards sees them. Restart Unity Hub and Unity if they are not picked up. This Editor session gets them immediately."
                : "This platform cannot save user variables from the Editor. They are set for this Editor session only. Use Copy commands and add them to your shell profile.";
            _filePath.text = EncryptionKeyFile.Path;

            _prefix.RegisterValueChangedCallback(evt => OnPrefixChanged(evt.newValue.Trim()));
            _source.Init(KeySource.Automatic);
            _source.RegisterValueChangedCallback(evt => _state.Edit("Change key source", c => c.keySource = (KeySource)evt.newValue));
            _previousClear.clicked += () => RunStorageAction(OnClearPrevious);
            view.Q<Button>("keys-panel__reload").clicked += OnReload;
            _envStore.clicked += () => RunStorageAction(() => StoreEnvironment(KeyScope.All));
            _envCopy.clicked += () => RunStorageAction(OnCopyCommands);
            view.Q<Button>("keys-panel__env-remove").clicked += () => RunStorageAction(OnRemoveEnvironment);
            _fileStore.clicked += () => RunStorageAction(() => StoreFile(KeyScope.All));
            _fileReveal.clicked += () => EditorUtility.RevealInFinder(File.Exists(EncryptionKeyFile.Path) ? EncryptionKeyFile.Path : Path.GetDirectoryName(EncryptionKeyFile.Path));
            _fileDelete.clicked += () => RunStorageAction(OnDeleteFile);
            _gameJson.clicked += () => ExportGameKeys(false);
            _gameEmbedded.clicked += () => ExportGameKeys(true);
            _gameSelect.clicked += () => SelectAsset(GameKeyExport.JsonPath);

            _state.Changed += Refresh;
            Refresh();
            return view;
        }

        public void OnSelected() => Refresh();

        private void RunStorageAction(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex) when (PanelUi.IsExpectedFailure(ex))
            {
                PanelUi.SetText(_result, "Key storage operation failed: " + PanelUi.ErrorText(ex), "error");
            }
        }

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
            _prefixHint.text = $"Variable: {EncryptionKeys.AesVariable(prefix)}. Environment delivery carries the current key only; use the key file or game export to retain previous keys.";

            EncryptionKeys keys = _state.Keys;
            EncryptionKeys onFile = new();
            string fileError = null;
            try
            {
                onFile = EncryptionKeyFile.Load() ?? new EncryptionKeys();
            }
            catch (Exception ex) when (PanelUi.IsExpectedFailure(ex))
            {
                fileError = "Project key file was rejected: " + PanelUi.ErrorText(ex);
            }

            if (fileError != null || _state.KeyLoadError != null)
            {
                PanelUi.SetText(_result, fileError ?? _state.KeyLoadError, "error");
            }

            ShowRow(_aesRow, keys.HasAesKey, onFile.HasAesKey, EncryptionKeys.AesVariable(prefix));
            bool hasKeys = config.encryptionMode == EncryptionMode.Aes && keys.HasAesKey;
            _envStore.SetEnabled(hasKeys);
            _fileStore.SetEnabled(hasKeys);
            _envCopy.SetEnabled(hasKeys);
            _gameJson.SetEnabled(hasKeys);
            _gameEmbedded.SetEnabled(hasKeys);
            _gameSelect.SetEnabled(GameKeyExport.JsonExists);
            _gameHint.text = $"JSON: {GameKeyExport.JsonPath}\nC#: {ShippingGenerator.DefaultFolder}/{ShippingGenerator.ScriptName}\n"
                             + "Exports the AES key and previous keys so the game can save and load encrypted files offline.";

            _source.SetValueWithoutNotify(config.keySource);
            RefreshActiveKey(config);
            int previous = keys.PreviousAesKeys.Count;
            PanelUi.SetVisible(_previousClear, previous > 0);
            _previousClear.text = $"Clear {previous} previous AES key(s)";

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

        private void RefreshActiveKey(DataIOConfig config)
        {
            if (config.encryptionMode != EncryptionMode.Aes)
            {
                PanelUi.SetChip(_active, "Unsupported saved encryption mode", PanelUi.Missing);
                _activeDetail.text = "Set up AES first. The saved mode has not been changed.";
                return;
            }

            if (config.format != DataFormat.Encrypted)
            {
                PanelUi.SetChip(_active, "Not encrypted", PanelUi.Warn);
                _activeDetail.text = "The format is not Encrypted, so DataIO does not read keys. Choose Encrypted on the Format tab.";
                return;
            }

            if (config.keySource == KeySource.Provider && !Application.isPlaying)
            {
                PanelUi.SetChip(_active, "Registers at runtime", PanelUi.Warn);
                _activeDetail.text = "A provider (the embedded setup, a token flow or your code) supplies its keys when Play mode or a build starts, so they cannot be checked here.";
                return;
            }

            try
            {
                EncryptionKeys active = EncryptionKeys.Resolve(config.environmentPrefix, config.keySource, out string origin);
                bool ready = active != null && active.CanWrite(config.encryptionMode);
                PanelUi.SetChip(_active, ready ? "Keys found" : "No usable key", ready ? PanelUi.Ok : PanelUi.Missing);
                _activeDetail.text = (active ?? new EncryptionKeys()).Describe(config.encryptionMode, origin);
            }
            catch (Exception ex) when (PanelUi.IsExpectedFailure(ex))
            {
                PanelUi.SetChip(_active, "Key source failed", PanelUi.Missing);
                _activeDetail.text = PanelUi.ErrorText(ex);
            }
        }

        private void OnClearPrevious()
        {
            if (!PanelUi.Confirm("Clear previous AES keys?",
                    "Saves written with an older key can no longer be opened once the keys are gone. Clear them only after every old save was loaded and saved again. "
                    + "Export the game key again afterwards so builds stop carrying the old keys.",
                    "Clear", "Cancel"))
            {
                return;
            }

            EncryptionKeys keys = _state.Keys.Clone();
            keys.PreviousAesKeys.Clear();
            _state.SetKeys(keys, _state.KeysUnstored);
            if (EncryptionKeyFile.Exists && keys.HasAesKey)
            {
                KeyStorage.SaveFile(KeyStorage.ForMode(keys, EncryptionMode.Aes), KeyScope.All);
            }

            PanelUi.SetText(_result, "Previous AES keys cleared. Export the game key again so builds stop carrying them.");
        }

        private EncryptionKeys KeysForMode() => KeyStorage.ForMode(_state.Keys, _state.Config.encryptionMode);

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
            PanelUi.SetText(_result, _state.KeyLoadError ?? "Keys reloaded from storage.", _state.KeyLoadError == null ? null : "error");
        }

        private void StoreEnvironment(KeyScope scope)
        {
            EncryptionKeys keys = KeysForMode();
            KeyStorage.SaveEnvironment(_state.Config.environmentPrefix, keys, scope);
            if (keys.PreviousAesKeys.Count == 0)
            {
                _state.MarkKeysStored();
            }
            string suffix = KeyStorage.CanPersistEnvironment ? " Restart other programs to see them." : " They last until the Editor closes.";
            PanelUi.SetText(_result, "AES key stored as an environment variable (previous keys are not included)." + suffix
                + (keys.PreviousAesKeys.Count > 0 ? " Save the full key ring to the project key file or export it before closing." : string.Empty));
        }

        private void OnCopyCommands()
        {
            EncryptionKeys keys = KeysForMode();
            if (!EditorUtility.DisplayDialog("Copy the AES key?", "The commands contain the AES key in plain text. Paste them only into a trusted shell or secret store.", "Copy", "Cancel"))
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
            PanelUi.SetText(_result, "AES key and previous keys saved to the project key file.");
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
            string path = embedded ? ShippingGenerator.DefaultFolder + "/" + ShippingGenerator.ScriptName : GameKeyExport.JsonPath;
            if (!PanelUi.Confirm("Export game key?",
                    $"This writes the AES key to {path} and includes it in builds. "
                    + "Any existing file at this path will be replaced. Older AES keys are kept so existing saves still open."
                    + " Anyone with the game can recover this key. Keep a separate backup and do not commit the generated file.",
                    "Export", "Cancel"))
            {
                return;
            }

            try
            {
                string error = GameKeyExport.Export(_state, embedded, ShippingGenerator.DefaultFolder, out path);
                if (error != null)
                {
                    PanelUi.SetText(_gameResult, error, "error");
                    return;
                }

                Refresh();
                PanelUi.SetText(_gameResult, $"Created {path}. Key source set to {_state.Config.keySource}. Restart Play mode to use it. "
                    + $"Use the Setup tab to generate the code that creates the store.");
                SelectAsset(path);
            }
            catch (Exception ex) when (PanelUi.IsExpectedFailure(ex))
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
