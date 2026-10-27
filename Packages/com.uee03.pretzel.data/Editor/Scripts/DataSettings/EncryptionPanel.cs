using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Pretzel.Data.Editor
{
    internal sealed class EncryptionPanel : IDataFormatPanel
    {
        private DataFormatState _state;
        private VisualElement _view;
        private Label _unstored;
        private Label _status;
        private Button _generate;
        private Button _copyAes;
        private Button _rotate;

        private KeyRow _aesRow;

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

            _unstored = _view.Q<Label>("encryption-panel__unstored");
            _status = _view.Q<Label>("encryption-panel__status");
            _generate = _view.Q<Button>("encryption-panel__generate");
            _copyAes = _view.Q<Button>("encryption-panel__copy-aes");
            _rotate = _view.Q<Button>("encryption-panel__rotate");

            VisualElement rows = _view.Q("encryption-panel__key-rows");
            _aesRow = AddRow(rows, "AES key");
            PanelUi.FillBullets(_view.Q("encryption-panel__mode-notes"), DataFormatInfo.AesModeNotes);
            PanelUi.FillBullets(_view.Q("encryption-panel__checklist"), DataFormatInfo.EncryptedChecklist, true);
            _generate.clicked += OnGenerate;
            _rotate.clicked += OnGenerate;
            _view.Q<Button>("encryption-panel__best-practices").clicked += OnOpenBestPractices;
            _copyAes.clicked += () => CopyKey(_state.Keys.HasAesKey ? EncryptionKeys.EncodeAesKey(_state.Keys.AesKey) : null);

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

            EncryptionKeys keys = _state.Keys;
            ShowRow(_aesRow, keys.HasAesKey, keys.HasAesKey ? PanelUi.Fingerprint(keys.AesKey) : null);
            _copyAes.SetEnabled(keys.HasAesKey);
            _generate.SetEnabled(!keys.HasAesKey && config.encryptionMode == EncryptionMode.Aes);
            _rotate.SetEnabled(keys.HasAesKey && config.encryptionMode == EncryptionMode.Aes);
            if (_state.KeyLoadError != null || config.encryptionMode != EncryptionMode.Aes)
            {
                PanelUi.SetText(_status, _state.KeyLoadError ?? "Unsupported saved encryption mode. Set up AES on the Setup tab.", "error");
                return;
            }

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
            if (_state.Config.encryptionMode != EncryptionMode.Aes)
            {
                PanelUi.SetText(_status, "Unsupported saved encryption mode. Set up AES on the Setup tab.", "error");
                return;
            }

            EncryptionKeys current = _state.Keys;
            if (current.HasAesKey && !PanelUi.Confirm(
                    "Rotate the AES key?",
                    "Optional: the current key is retained for reading old saves. New writes use the new key. "
                    + "Back up the keys and export again after rotation. Clear previous keys only when no old saves remain.",
                    "Rotate", "Cancel"))
            {
                return;
            }

            EncryptionKeys next = current.Clone();
            byte[] replaced = current.AesKey;
            next.AesKey = EncryptionKeys.GenerateAesKey();
            next.AddPreviousAesKey(replaced);
            _state.SetKeys(next, true);
            PanelUi.SetText(_status, "AES key created in memory. Store and back it up on the Keys tab; export it before using it in a build.");
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

        private void CopyKey(string text)
        {
            if (text == null)
            {
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Copy the AES key?",
                    "The key will be on your clipboard in plain text. Paste it only into a secret manager or trusted tool, then clear the clipboard.",
                    "Copy", "Cancel"))
            {
                return;
            }

            PanelUi.Copy(text);
            _status.text = "AES key copied (Base64).";
        }
    }
}
