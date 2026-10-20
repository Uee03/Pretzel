using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Data.Editor
{
    /// <summary>Two buttons that generate ProjectDataIO.cs: one for plain, GZip and BSON, one for AES or RSA encryption.</summary>
    internal sealed class SetupPanel : IDataFormatPanel
    {
        private static readonly DataFormat[] PlainFormats = { DataFormat.PlainText, DataFormat.Compressed, DataFormat.Binary };
        private static readonly string[] PlainChoices = { "Plain JSON (.json)", "GZip (.json.gz)", "BSON (.bin)" };
        private static readonly string[] ModeChoices = { "AES: the game reads and writes", "RSA: the game writes, a server reads" };
        private static readonly string[] DeliveryChoices = { "Embedded C# (obfuscated, recommended)", "Bundled JSON (Resources)" };

        private DataFormatState _state;
        private TextField _folder;
        private Label _current;
        private Label _currentDetail;
        private Label _result;
        private DropdownField _plainFormat;
        private DropdownField _mode;
        private DropdownField _delivery;
        private HelpBox _note;

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
            _mode = view.Q<DropdownField>("setup-panel__mode");
            _delivery = view.Q<DropdownField>("setup-panel__delivery");
            _note = view.Q<HelpBox>("setup-panel__encryption-note");

            _folder.SetValueWithoutNotify(DataIOSetupGenerator.DefaultFolder);
            _plainFormat.choices = new List<string>(PlainChoices);
            _mode.choices = new List<string>(ModeChoices);
            _delivery.choices = new List<string>(DeliveryChoices);

            // Start from what the project already uses, then leave the choices to the user.
            DataIOConfig config = _state.Config;
            int plain = Array.IndexOf(PlainFormats, config.format);
            _plainFormat.index = Mathf.Max(0, plain);
            _mode.index = config.encryptionMode == EncryptionMode.Rsa ? 1 : 0;
            _delivery.index = config.keySource == KeySource.BundledJson ? 1 : 0;

            _folder.RegisterValueChangedCallback(_ => Refresh());
            _mode.RegisterValueChangedCallback(_ => Refresh());
            view.Q<Button>("setup-panel__plain-generate").clicked += OnGeneratePlain;
            view.Q<Button>("setup-panel__encrypted-generate").clicked += OnGenerateEncrypted;
            view.Q<Button>("setup-panel__examples").clicked += OnAddExamples;
            view.Q<Button>("setup-panel__validate").clicked += OnValidate;

            _state.Changed += Refresh;
            Refresh();
            return view;
        }

        public void OnSelected() => Refresh();

        private string Folder => _folder.value.Trim().Replace('\\', '/').TrimEnd('/');

        private EncryptionMode SelectedMode => _mode.index == 1 ? EncryptionMode.Rsa : EncryptionMode.Aes;

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
                PanelUi.SetChip(_current, matches ? "ProjectDataIO.cs matches the settings" : "ProjectDataIO.cs does not match the settings",
                    matches ? PanelUi.Ok : PanelUi.Missing);
                _currentDetail.text = $"{path} is the {(encryptedScript ? "encryption" : "plain / GZip / BSON")} setup. "
                    + $"The settings asset says: {config.format}"
                    + (config.format == DataFormat.Encrypted ? $" ({config.encryptionMode}, key source {config.keySource})" : string.Empty)
                    + (matches ? "." : ". Generate the matching setup, or change the format on the Format tab.");
            }

            bool rsa = SelectedMode == EncryptionMode.Rsa;
            _note.messageType = rsa ? HelpBoxMessageType.Info : HelpBoxMessageType.Warning;
            _note.text = rsa
                ? "RSA: the game only gets the public key and cannot read its own files. Use it to send data to a server. The private key stays in the Editor-only key file and is never exported."
                : "A key that ships with the game can be recovered by a determined player, with or without IL2CPP. This stops casual save editing, not a cheater. Keep real secrets on a server.";
        }

        private void OnGeneratePlain()
        {
            DataFormat format = PlainFormats[Mathf.Clamp(_plainFormat.index, 0, PlainFormats.Length - 1)];
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
                Succeed($"Created {path} for {format}. Call ProjectDataIO.Initialize() once at startup, then use DataIO.Save and DataIO.Load.", path);
            }
            catch (Exception ex) when (IsExpected(ex))
            {
                PanelUi.SetText(_result, "Could not generate the setup: " + PanelUi.ErrorText(ex), "error");
            }
        }

        private void OnGenerateEncrypted()
        {
            EncryptionMode mode = SelectedMode;
            bool embedded = _delivery.index == 0;
            KeySource source = embedded ? KeySource.Provider : KeySource.BundledJson;
            try
            {
                if (mode == EncryptionMode.Rsa && !_state.Keys.HasPublicKey)
                {
                    PanelUi.SetText(_result, "Generate an RSA key pair on the Encryption tab first. It can take a while, so it is not done here.", "error");
                    return;
                }

                // Fail before changing anything: a bad folder or a duplicate script must not leave a half-done setup.
                ShippingGenerator.ValidateFolder(Folder);
                ShippingGenerator.EnsureNoOtherScriptCopy(DataIOSetupGenerator.ScriptName, DataIOSetupGenerator.ScriptPath(Folder));
                if (embedded)
                {
                    ShippingGenerator.EnsureNoOtherScriptCopy(ShippingGenerator.ScriptName, Folder + "/" + ShippingGenerator.ScriptName);
                }

                string code = DataIOSetupGenerator.RenderEncrypted(mode, source);
                if (!ConfirmEncrypted(mode, embedded, DataIOSetupGenerator.WouldReplace(Folder, code)))
                {
                    return;
                }

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

                // The Editor-only key file is the project's own copy of the keys, including an RSA private key.
                if (_state.KeysUnstored)
                {
                    _state.SaveKeysToFile();
                }

                string error = GameKeyExport.Export(_state, embedded, Folder, out string keyPath);
                if (error != null)
                {
                    PanelUi.SetText(_result, error, "error");
                    return;
                }

                string path = DataIOSetupGenerator.Write(Folder, code);
                Succeed($"Created {path} and exported the key to {keyPath}. Key source: {_state.Config.keySource}. "
                    + "Call ProjectDataIO.Initialize() once at startup, then use DataIO.Save and DataIO.Load. Back up the key.", path);
            }
            catch (Exception ex) when (IsExpected(ex))
            {
                PanelUi.SetText(_result, "Could not generate the setup: " + PanelUi.ErrorText(ex), "error");
            }
        }

        private void OnAddExamples()
        {
            try
            {
                List<string> created = DataIOSetupGenerator.CreateExamples(Folder, _state.Config.format == DataFormat.Encrypted);
                PanelUi.SetText(_result, created.Count == 0
                    ? "Every example already exists, so nothing was replaced."
                    : $"Added {created.Count} file(s) to {Folder}: {string.Join(", ", created.ConvertAll(file => Path.GetFileName(file)))}. "
                      + $"Read {DataIOSetupGenerator.GuideName} next. Add ProjectSaveExample to a GameObject to try it in Play mode.");
            }
            catch (Exception ex) when (IsExpected(ex))
            {
                PanelUi.SetText(_result, "Could not add the examples: " + PanelUi.ErrorText(ex), "error");
            }
        }

        private void OnValidate()
        {
            List<BuildIssue> issues = DataIOBuildValidator.Validate();
            if (issues.Count == 0)
            {
                PanelUi.SetText(_result, "Build setup looks good: nothing to fix.");
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
            string key = mode == EncryptionMode.Aes
                ? (_state.Keys.HasAesKey ? "use the current AES key" : "create a new AES key")
                : "use the current RSA key pair";
            string shipped = mode == EncryptionMode.Aes ? "the AES key (anyone with the game can recover it)" : "only the RSA public key";
            return PanelUi.Confirm("Set up encrypted DataIO?",
                "This will:\n"
                + $"- switch the settings asset to Encrypted ({mode})\n"
                + $"- {key}, and save it to the Editor-only key file\n"
                + $"- write {shipped} to {target}, which ships with the game\n"
                + $"- set the key source to {(embedded ? KeySource.Provider : KeySource.BundledJson)}\n"
                + $"- write ProjectDataIO.cs{(replacing ? " (replacing the existing, different file)" : string.Empty)}\n\n"
                + "Keep a separate backup of the key. Files encrypted with it cannot be opened without it.",
                "Set up", "Cancel");
        }

        private static bool IsExpected(Exception ex)
        {
            return ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException
                or System.Security.Cryptography.CryptographicException or System.Xml.XmlException;
        }
    }
}
