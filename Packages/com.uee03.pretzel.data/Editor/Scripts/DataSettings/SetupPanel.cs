using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Data.Editor
{
    /// <summary>Generates project setup and independent examples for each supported use case.</summary>
    internal sealed class SetupPanel : IDataFormatPanel
    {
        private static readonly DataFormat[] _plainFormats = { DataFormat.PlainText, DataFormat.Binary };
        private static readonly string[] _plainChoices = { "Plain JSON (.json)", "BinaryWriter / BinaryReader (.bin)" };
        private static readonly string[] _deliveryChoices = { "Embedded C# (obfuscated)", "Bundled JSON (Resources)" };

        private DataFormatState _state;
        private TextField _folder;
        private Label _current;
        private Label _currentDetail;
        private Label _result;
        private DropdownField _plainFormat;
        private DropdownField _delivery;
        private HelpBox _note;
        private Label _summary;

        [Serializable]
        private sealed class DiagnosticData
        {
            public string token;
            public int number;
        }

        public string Title => "Setup";

        public VisualElement CreateView(DataFormatState state)
        {
            _state = state;
            VisualElement view = DataEditorAssets.Layout("SetupTab").Instantiate();
            view.style.flexGrow = 1f;

            _folder = view.Q<TextField>("setup-panel__folder");
            _current = view.Q<Label>("setup-panel__current");
            _currentDetail = view.Q<Label>("setup-panel__current-detail");
            _result = view.Q<Label>("setup-panel__result");
            _plainFormat = view.Q<DropdownField>("setup-panel__plain-format");
            _delivery = view.Q<DropdownField>("setup-panel__delivery");
            _note = view.Q<HelpBox>("setup-panel__encryption-note");
            _summary = view.Q<Label>("setup-panel__summary");

            _folder.SetValueWithoutNotify(DataIOSetupGenerator.DefaultFolder);
            _plainFormat.choices = new List<string>(_plainChoices);
            _delivery.choices = new List<string>(_deliveryChoices);

            // Start from what the project already uses, then leave the choices to the user.
            DataIOConfig config = _state.Config;
            int plain = Array.IndexOf(_plainFormats, config.format);
            _plainFormat.index = Mathf.Max(0, plain);
            _delivery.index = config.keySource == KeySource.BundledJson ? 1 : 0;

            _folder.RegisterValueChangedCallback(_ => Refresh());
            view.Q<Button>("setup-panel__plain-generate").clicked += OnGeneratePlain;
            view.Q<Button>("setup-panel__encrypted-generate").clicked += OnGenerateEncrypted;
            view.Q<Button>("setup-panel__examples").clicked += OnAddExamples;
            view.Q<Button>("setup-panel__validate").clicked += OnValidate;
            view.Q<Button>("setup-panel__plain-example").clicked += () => OnUseCaseExample(DataFormat.PlainText);
            view.Q<Button>("setup-panel__binary-example").clicked += () => OnUseCaseExample(DataFormat.Binary);
            view.Q<Button>("setup-panel__aes-example").clicked += () => OnUseCaseExample(DataFormat.Encrypted, EncryptionMode.Aes);
            view.Q<Button>("setup-panel__test").clicked += OnTestSaveAndLoad;
            view.Q<Button>("setup-panel__open-store").clicked += OnOpenStore;

            _state.Changed += Refresh;
            Refresh();
            return view;
        }

        public void OnSelected() => Refresh();

        private string Folder => _folder.value.Trim().Replace('\\', '/').TrimEnd('/');

        private void Refresh()
        {
            DataIOConfig config = _state.Config;
            if (!config)
            {
                return;
            }

            string path = DataIOSetupGenerator.ScriptPath(Folder);
            if (!File.Exists(path))
            {
                PanelUi.SetChip(_current, "No setup code yet", PanelUi.Warn);
                _currentDetail.text = $"Use one of the two buttons to create {path}.";
            }
            else
            {
                bool encryptedScript = File.ReadAllText(path).Contains("InitializeWithKeys");
                bool matches = encryptedScript == (config.format == DataFormat.Encrypted);
                PanelUi.SetChip(_current, matches ? "Setup family detected (not validated)" : "Setup family differs from settings",
                    matches ? PanelUi.Warn : PanelUi.Missing);
                _currentDetail.text = $"{path} is the {(encryptedScript ? "encryption" : "plain / binary")} setup. "
                    + $"The settings asset says: {config.format}"
                    + (config.format == DataFormat.Encrypted ? $" ({config.encryptionMode}, key source {config.keySource})" : string.Empty)
                    + (matches ? ". Source inspection does not validate overrides, folder, keys or runtime startup." : ". Generate the matching setup, or change the format on the Format tab.");
            }

            bool supported = config.encryptionMode == EncryptionMode.Aes;
            _note.messageType = supported ? HelpBoxMessageType.Warning : HelpBoxMessageType.Error;
            _note.text = supported
                ? "AES is the only encryption choice. A shipped key can be recovered by a determined player. This stops casual save editing, not cheating. Keep real secrets on a server."
                : "Unsupported saved encryption mode. It has not been changed. Generate an AES setup to replace it.";
            string storeFolder;
            try
            {
                storeFolder = StoreFolder(config);
            }
            catch (ArgumentException ex)
            {
                storeFolder = "Invalid folder: " + ex.Message;
            }

            _summary.text = $"Store folder: {storeFolder}\nFormat: {config.format} (.{config.fileExtension})"
                + $"\nKey source: {(config.format == DataFormat.Encrypted ? config.keySource.ToString() : "None — no key needed")}"
                + "\nReadiness: not verified by source inspection. Test save and load checks this Editor's settings and configured keys only; player delivery and startup still need a player test."
                + (supported || config.format != DataFormat.Encrypted ? string.Empty : "\nBlocked: unsupported saved encryption mode.")
                + (_state.KeyLoadError == null ? string.Empty : "\n" + _state.KeyLoadError);
        }

        private void OnGeneratePlain()
        {
            DataFormat format = _plainFormats[Mathf.Clamp(_plainFormat.index, 0, _plainFormats.Length - 1)];
            try
            {
                string source = DataIOSetupGenerator.RenderPlain(format);
                if (DataIOSetupGenerator.WouldReplace(Folder, source) && !ConfirmReplace())
                {
                    return;
                }

                string path = DataIOSetupGenerator.Write(Folder, source);
                _state.Edit("Set up plain DataIO", c =>
                {
                    c.format = format;
                    c.fileExtension = DataFormats.DefaultExtension(format);
                });
                Succeed($"Created {path} for {format}. It initializes itself before the first scene loads. Add the examples for a ready save manager.", path);
            }
            catch (Exception ex) when (PanelUi.IsExpectedFailure(ex))
            {
                PanelUi.SetText(_result, "Could not generate the setup: " + PanelUi.ErrorText(ex), "error");
            }
        }

        private void OnGenerateEncrypted()
        {
            const EncryptionMode mode = EncryptionMode.Aes;
            bool embedded = _delivery.index == 0;
            KeySource source = embedded ? KeySource.Provider : KeySource.BundledJson;
            DataIOConfig before = null;
            bool completed = false;
            try
            {
                // Fail before changing anything: a bad folder or a duplicate script must not leave a half-done setup.
                ShippingGenerator.ValidateFolder(Folder);
                ShippingGenerator.EnsureNoOtherScriptCopy(DataIOSetupGenerator.ScriptName, DataIOSetupGenerator.ScriptPath(Folder));
                if (embedded)
                {
                    ShippingGenerator.EnsureNoOtherScriptCopy(ShippingGenerator.ScriptName, Folder + "/" + ShippingGenerator.ScriptName);
                }

                string code = DataIOSetupGenerator.RenderEncrypted(mode, source);
                if (embedded && !ShippingGenerator.ConfirmReplace(Folder))
                {
                    return;
                }

                if (!ConfirmEncrypted(mode, embedded, DataIOSetupGenerator.WouldReplace(Folder, code)))
                {
                    return;
                }

                before = UnityEngine.Object.Instantiate(_state.Config);
                _state.Edit("Set up encrypted DataIO", c =>
                {
                    c.format = DataFormat.Encrypted;
                    c.fileExtension = DataFormats.DefaultExtension(DataFormat.Encrypted);
                    c.encryptionMode = mode;
                });

                if (mode == EncryptionMode.Aes && !_state.Keys.HasAesKey)
                {
                    EncryptionKeys created = _state.Keys.Clone();
                    created.AesKey = EncryptionKeys.GenerateAesKey();
                    _state.SetKeys(created, true);
                }

                if (_state.KeysUnstored)
                {
                    _state.SaveKeysToFile();
                }

                string error = GameKeyExport.Export(_state, embedded, Folder, out string keyPath, replaceConfirmed: true);
                if (error != null)
                {
                    PanelUi.SetText(_result, error, "error");
                    return;
                }

                string path = DataIOSetupGenerator.Write(Folder, code);
                completed = true;
                Succeed($"Created {path} and exported the key to {keyPath}. Key source: {_state.Config.keySource}. "
                    + "It loads the key and initializes itself before the first scene loads. Back up the key.", path);
            }
            catch (Exception ex) when (PanelUi.IsExpectedFailure(ex))
            {
                PanelUi.SetText(_result, "Could not generate the setup: " + PanelUi.ErrorText(ex), "error");
            }
            finally
            {
                if (before)
                {
                    // A failed setup must not leave the settings switched to Encrypted. Stored keys are kept.
                    if (!completed)
                    {
                        RestoreConfig(before);
                    }

                    UnityEngine.Object.DestroyImmediate(before);
                }
            }
        }

        private void RestoreConfig(DataIOConfig before)
        {
            _state.Edit("Revert encrypted DataIO setup", c =>
            {
                c.format = before.format;
                c.fileExtension = before.fileExtension;
                c.encryptionMode = before.encryptionMode;
                c.keySource = before.keySource;
            });
        }

        private void OnAddExamples()
        {
            try
            {
                List<string> created = DataIOSetupGenerator.CreateExamples(Folder, _state.Config.format == DataFormat.Encrypted);
                PanelUi.SetText(_result, created.Count == 0
                    ? "Every example already exists, so nothing was replaced."
                    : $"Added {created.Count} file(s) to {Folder}: {string.Join(", ", created.ConvertAll(file => Path.GetFileName(file)))}. "
                      + $"Read {DataIOSetupGenerator.GuideName} next. Add ProjectSaveManager to a GameObject in your first scene.");
            }
            catch (Exception ex) when (PanelUi.IsExpectedFailure(ex))
            {
                PanelUi.SetText(_result, "Could not add the examples: " + PanelUi.ErrorText(ex), "error");
            }
        }

        private void OnUseCaseExample(DataFormat format, EncryptionMode mode = EncryptionMode.Aes)
        {
            try
            {
                string path = DataIOSetupGenerator.CreateUseCaseExample(Folder, format, mode);
                Succeed($"Created {path}. Add the component to a GameObject, enter Play mode and use its Run example context menu. "
                    + "It uses its own example folder and does not change the active DataIO setup.", path);
            }
            catch (Exception ex) when (PanelUi.IsExpectedFailure(ex))
            {
                PanelUi.SetText(_result, "Could not generate the example: " + PanelUi.ErrorText(ex), "error");
            }
        }

        private void OnValidate()
        {
            List<BuildIssue> issues = DataIOBuildValidator.Validate();
            if (issues.Count == 0)
            {
                PanelUi.SetText(_result, "No issues found by the limited build-delivery checks. This does not prove startup code, keys or player save/load work.");
                return;
            }

            StringBuilder text = new();
            bool error = false;
            foreach (BuildIssue issue in issues)
            {
                text.AppendLine(issue.ToString());
                error |= issue.Severity == IssueSeverity.Error;
            }

            PanelUi.SetText(_result, text.ToString().TrimEnd(), error ? "error" : "warning");
        }

        private void Succeed(string message, string path)
        {
            Refresh();
            PanelUi.SetText(_result, message);
            UnityEngine.Object script = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (script)
            {
                EditorGUIUtility.PingObject(script);
            }
        }

        private static bool ConfirmReplace()
        {
            return PanelUi.Confirm("Replace ProjectDataIO.cs?",
                "The existing ProjectDataIO.cs is different, for example a different setup or your edits. Replace it?", "Replace", "Cancel");
        }

        private bool ConfirmEncrypted(EncryptionMode mode, bool embedded, bool replacing)
        {
            string target = embedded ? Folder + "/" + ShippingGenerator.ScriptName : GameKeyExport.JsonPath;
            string key = _state.Keys.HasAesKey ? "use the current AES key" : "create a new AES key";
            const string shipped = "the AES key (anyone with the game can recover it)";
            return PanelUi.Confirm("Set up encrypted DataIO?",
                "This will:\n"
                + $"- switch the settings asset to Encrypted ({mode})\n"
                + $"- {key}, and save it to the Editor-only key file\n"
                + $"- write {shipped} to {target}, which ships with the game\n"
                + $"- set the key source to {(embedded ? KeySource.Provider : KeySource.BundledJson)}\n"
                + $"- write ProjectDataIO.cs{(replacing ? " (replacing the existing, different file)" : string.Empty)}\n\n"
                + "Keep a separate backup of the key. Files encrypted with it cannot be opened without it."
                + (_state.Config.encryptionMode == EncryptionMode.Aes ? string.Empty
                    : "\n\nThis explicitly replaces an unsupported saved mode with AES."),
                "Set up", "Cancel");
        }

        internal static string StoreFolder(DataIOConfig config)
        {
            return DataRoots.PersistentData(string.IsNullOrWhiteSpace(config.saveFolder) ? null : config.saveFolder.Trim('/', '\\')).FullPath;
        }

        internal static string TestSaveAndLoad(DataIOConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            // Never resolve the configured player root or touch DataIO.Default.
            string folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "Library", "Pretzel", "DataIODiagnostics", Guid.NewGuid().ToString("N"));
            try
            {
                DataStore store = config.CreateStore(DataRoots.Absolute(folder));
                DiagnosticData expected = new() { token = Guid.NewGuid().ToString("N"), number = 173 };
                const string key = "pretzel-diagnostic";
                store.Save(key, expected);
                LoadResult<DiagnosticData> loaded = store.Load<DiagnosticData>(key);
                if (!loaded.Ok || loaded.Value == null || loaded.Value.token != expected.token || loaded.Value.number != expected.number)
                {
                    throw new InvalidDataException($"Diagnostic round trip failed ({loaded.Status}): {loaded.Error ?? "payload mismatch"}");
                }

                return "Test save and load passed in an isolated diagnostic store using the current Editor settings and configured key source. "
                    + "Diagnostic files were removed. Player profiles were untouched. This does not verify player key delivery or startup code.";
            }
            finally
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
        }

        private void OnTestSaveAndLoad()
        {
            try
            {
                PanelUi.SetText(_result, TestSaveAndLoad(_state.Config));
            }
            catch (Exception ex) when (PanelUi.IsExpectedFailure(ex))
            {
                PanelUi.SetText(_result, "Test save and load failed: " + PanelUi.ErrorText(ex)
                    + ". No key was generated. Configure the existing key source first.", "error");
            }
        }

        private void OnOpenStore()
        {
            try
            {
                string folder = StoreFolder(_state.Config);
                if (!Directory.Exists(folder))
                {
                    PanelUi.SetText(_result, $"Store folder has not been created: {folder}. The diagnostic test intentionally does not create it.", "warning");
                    return;
                }

                EditorUtility.RevealInFinder(folder);
            }
            catch (Exception ex) when (PanelUi.IsExpectedFailure(ex))
            {
                PanelUi.SetText(_result, "Could not open the configured store folder: " + PanelUi.ErrorText(ex), "error");
            }
        }

    }
}
