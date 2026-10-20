using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine.UIElements;

namespace Pretzel.Data.Editor
{
    internal sealed class EncryptionPanel : IDataFormatPanel
    {
        private static readonly string[] ModeChoices =
        {
            "AES-256 (one secret key)",
            "RSA public key + AES-256 (hybrid)"
        };

        private DataFormatState _state;
        private VisualElement _view;
        private DropdownField _mode;
        private VisualElement _modeNotes;
        private VisualElement _rsaSizeRow;
        private DropdownField _rsaSize;
        private Label _rsaSizeNote;
        private VisualElement _privateSection;
        private Label _unstored;
        private Label _status;
        private Button _generate;
        private Button _copyAes;
        private Button _copyPublic;
        private Button _copyPrivate;

        private KeyRow _aesRow;
        private KeyRow _publicRow;
        private KeyRow _privateRow;

        private EncryptionMode? _notesShown;
        private bool _generating;
        private IVisualElementScheduledItem _poll;

        private sealed class KeyRow
        {
            public VisualElement Root;
            public Label Chip;
            public Label Detail;
        }

        public string Title => "Encryption";

        public VisualElement CreateView(DataFormatState state)
        {
            _state = state;
            _view = DataEditorAssets.Layout("EncryptionTab").Instantiate();
            _view.style.flexGrow = 1f;

            _mode = _view.Q<DropdownField>("encryption-panel__mode");
            _modeNotes = _view.Q("encryption-panel__mode-notes");
            _rsaSizeRow = _view.Q("encryption-panel__rsa-size-row");
            _rsaSize = _view.Q<DropdownField>("encryption-panel__rsa-size");
            _rsaSizeNote = _view.Q<Label>("encryption-panel__rsa-size-note");
            _privateSection = _view.Q("encryption-panel__private-section");
            _unstored = _view.Q<Label>("encryption-panel__unstored");
            _status = _view.Q<Label>("encryption-panel__status");
            _generate = _view.Q<Button>("encryption-panel__generate");
            _copyAes = _view.Q<Button>("encryption-panel__copy-aes");
            _copyPublic = _view.Q<Button>("encryption-panel__copy-public");
            _copyPrivate = _view.Q<Button>("encryption-panel__copy-private");

            VisualElement rows = _view.Q("encryption-panel__key-rows");
            _aesRow = AddRow(rows, "AES key");
            _publicRow = AddRow(rows, "RSA public key");
            _privateRow = AddRow(rows, "RSA private key");

            _mode.choices = new List<string>(ModeChoices);
            string[] sizes = new string[EncryptionKeys.RsaKeySizes.Length];
            for (int i = 0; i < sizes.Length; i++)
            {
                int bits = EncryptionKeys.RsaKeySizes[i];
                sizes[i] = bits == EncryptionKeys.RecommendedRsaKeySize ? bits + " (recommended)" : bits.ToString();
            }

            _rsaSize.choices = new List<string>(sizes);

            PanelUi.FillBullets(_view.Q("encryption-panel__private-advice"), DataFormatInfo.PrivateKeyAdvice);
            PanelUi.FillBullets(_view.Q("encryption-panel__checklist"), DataFormatInfo.EncryptedChecklist, true);

            _mode.RegisterValueChangedCallback(_ =>
            {
                EncryptionMode mode = _mode.index == 1 ? EncryptionMode.Rsa : EncryptionMode.Aes;
                _state.Edit("Change encryption mode", c => c.encryptionMode = mode);
            });
            _rsaSize.RegisterValueChangedCallback(_ =>
            {
                int bits = EncryptionKeys.RsaKeySizes[Math.Max(0, _rsaSize.index)];
                _state.Edit("Change RSA key size", c => c.rsaKeySize = bits);
            });

            _generate.clicked += OnGenerate;
            _view.Q<Button>("encryption-panel__best-practices").clicked += OnOpenBestPractices;
            _copyAes.clicked += () => CopyKey(_state.Keys.HasAesKey ? EncryptionKeys.EncodeAesKey(_state.Keys.AesKey) : null, "AES key", false);
            _copyPublic.clicked += () => CopyKey(EncryptionKeys.EncodeXml(_state.Keys.RsaPublicKey), "Public key", false);
            _copyPrivate.clicked += () => CopyKey(EncryptionKeys.EncodeXml(_state.Keys.RsaPrivateKey), "Private key", true);

            _state.Changed += Refresh;
            Refresh();
            return _view;
        }

        public void OnSelected() => Refresh();

        private static KeyRow AddRow(VisualElement parent, string name)
        {
            KeyRow row = new() { Root = new VisualElement(), Chip = new Label(), Detail = new Label() };
            row.Root.AddToClassList("pretzel-key-row");
            Label label = new(name);
            label.AddToClassList("pretzel-key-row__name");
            row.Chip.AddToClassList("pretzel-key-row__chip");
            row.Detail.AddToClassList("pretzel-key-row__detail");
            row.Root.Add(label);
            row.Root.Add(row.Chip);
            row.Root.Add(row.Detail);
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

            bool rsa = config.encryptionMode == EncryptionMode.Rsa;
            _mode.SetValueWithoutNotify(ModeChoices[rsa ? 1 : 0]);

            int sizeIndex = Math.Max(0, Array.IndexOf(EncryptionKeys.RsaKeySizes, config.rsaKeySize));
            _rsaSize.SetValueWithoutNotify(_rsaSize.choices[sizeIndex]);
            _rsaSizeNote.text = DataFormatInfo.RsaSizeNote(EncryptionKeys.RsaKeySizes[sizeIndex]);

            if (_notesShown != config.encryptionMode)
            {
                _notesShown = config.encryptionMode;
                PanelUi.FillBullets(_modeNotes, rsa ? DataFormatInfo.RsaModeNotes : DataFormatInfo.AesModeNotes);
            }

            PanelUi.SetVisible(_rsaSizeRow, rsa);
            PanelUi.SetVisible(_privateSection, rsa);
            PanelUi.SetVisible(_aesRow.Root, !rsa);
            PanelUi.SetVisible(_publicRow.Root, rsa);
            PanelUi.SetVisible(_privateRow.Root, rsa);
            PanelUi.SetVisible(_copyAes, !rsa);
            PanelUi.SetVisible(_copyPublic, rsa);
            PanelUi.SetVisible(_copyPrivate, rsa);

            EncryptionKeys keys = _state.Keys;
            ShowRow(_aesRow, keys.HasAesKey, keys.HasAesKey ? PanelUi.Fingerprint(keys.AesKey) : null);
            ShowRow(_publicRow, keys.HasPublicKey, keys.HasPublicKey ? PanelUi.Fingerprint(Encoding.UTF8.GetBytes(keys.RsaPublicKey)) : null);
            ShowRow(_privateRow, keys.HasPrivateKey, keys.HasPrivateKey ? PanelUi.Fingerprint(Encoding.UTF8.GetBytes(keys.RsaPrivateKey)) : null);

            _copyAes.SetEnabled(keys.HasAesKey);
            _copyPublic.SetEnabled(keys.HasPublicKey);
            _copyPrivate.SetEnabled(keys.HasPrivateKey);
            _generate.SetEnabled(!_generating);

            _unstored.text = _state.KeysUnstored
                ? "These keys are only in memory. Store them on the Keys tab before you close this window."
                : string.Empty;
            PanelUi.SetVisible(_unstored, _state.KeysUnstored);
        }

        private static void ShowRow(KeyRow row, bool present, string fingerprint)
        {
            PanelUi.SetChip(row.Chip, present ? "Available" : "Missing", present ? PanelUi.Ok : PanelUi.Missing);
            row.Detail.text = present ? "Fingerprint " + fingerprint : "Generate keys to create it.";
        }

        private void OnGenerate()
        {
            if (_generating)
            {
                return;
            }

            EncryptionKeys current = _state.Keys;
            bool rsa = _state.Config.encryptionMode == EncryptionMode.Rsa;
            bool replacing = rsa ? current.HasPublicKey || current.HasPrivateKey : current.HasAesKey;
            if (replacing && !PanelUi.Confirm(
                    rsa ? "Replace the key pair?" : "Replace the AES key?",
                    rsa
                        ? "Files encrypted with the current key pair cannot be opened with a new one. Back up the current private key first if you still need those files."
                        : "The current key is kept as a previous key, so saves written with it still open and move to the new key when they are saved again. "
                          + "Clear the previous keys on the Keys tab once no old saves remain.",
                    "Replace", "Cancel"))
            {
                return;
            }

            if (!rsa)
            {
                EncryptionKeys next = current.Clone();
                byte[] replaced = current.AesKey;
                next.AesKey = EncryptionKeys.GenerateAesKey();
                // Only after the swap: a key equal to the current one is ignored as a previous key.
                next.AddPreviousAesKey(replaced);
                _status.text = string.Empty;
                _state.SetKeys(next, true);
                return;
            }

            StartRsa(_state.Config.rsaKeySize);
        }

        private void StartRsa(int bits)
        {
            _generating = true;
            _generate.SetEnabled(false);
            PanelUi.SetText(_status, $"Generating a {bits}-bit RSA pair. A larger size takes longer, and this runs in the background.");

            Task<EncryptionKeys> task = Task.Run(() => EncryptionKeys.GenerateRsa(bits));
            _poll?.Pause();
            _poll = _view.schedule.Execute(() =>
            {
                if (!task.IsCompleted)
                {
                    return;
                }

                _poll.Pause();
                _generating = false;
                if (task.IsFaulted)
                {
                    PanelUi.SetText(_status, "Key generation failed: " + task.Exception.GetBaseException().Message, "error");
                    Refresh();
                    return;
                }

                EncryptionKeys next = _state.Keys.Clone();
                next.RsaPublicKey = task.Result.RsaPublicKey;
                next.RsaPrivateKey = task.Result.RsaPrivateKey;
                _status.text = string.Empty;
                _state.SetKeys(next, true);
            }).Every(200);
        }

        private void OnOpenBestPractices()
        {
            string path = ShippingGenerator.DocumentPath("EncryptionBestPractices.md");
            if (path != null && System.IO.File.Exists(path))
            {
                EditorUtility.OpenWithDefaultApp(path);
            }
            else
            {
                PanelUi.SetText(_status, "The guide was not found in the package.", "error");
            }
        }

        private void CopyKey(string text, string what, bool secret)
        {
            if (text == null)
            {
                return;
            }

            if (secret && !EditorUtility.DisplayDialog(
                    "Copy the private key?",
                    "The private key will be on your clipboard in plain text. Paste it only into a secret manager or a trusted tool, then clear the clipboard.",
                    "Copy", "Cancel"))
            {
                return;
            }

            PanelUi.Copy(text);
            _status.text = what + " copied (Base64).";
        }
    }
}
