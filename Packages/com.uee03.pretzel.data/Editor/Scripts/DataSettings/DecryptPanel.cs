using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine.UIElements;

namespace Pretzel.Data.Editor
{
    internal sealed class DecryptPanel : IDataFormatPanel
    {
        private static readonly string[] FormatChoices =
        {
            "Auto-detect", "Plain text JSON", "Compressed (GZip)", "Binary (BSON)", "Encrypted"
        };

        private static readonly DataFormat?[] FormatValues =
        {
            null, DataFormat.PlainText, DataFormat.Compressed, DataFormat.Binary, DataFormat.Encrypted
        };

        private DataFormatState _state;
        private DropdownField _format;
        private TextField _input;
        private TextField _output;
        private Label _status;
        private TextField _aes;
        private TextField _private;
        private TextField _public;

        public string Title => "Decrypt";

        public VisualElement CreateView(DataFormatState state)
        {
            _state = state;
            VisualElement view = DataEditorAssets.Layout("DecryptTab").Instantiate();
            view.style.flexGrow = 1f;

            _format = view.Q<DropdownField>("decrypt-panel__format");
            _input = view.Q<TextField>("decrypt-panel__input");
            _output = view.Q<TextField>("decrypt-panel__output");
            _status = view.Q<Label>("decrypt-panel__status");
            _aes = view.Q<TextField>("decrypt-panel__aes");
            _private = view.Q<TextField>("decrypt-panel__private");
            _public = view.Q<TextField>("decrypt-panel__public");

            _format.choices = new List<string>(FormatChoices);
            _format.SetValueWithoutNotify(FormatChoices[0]);

            view.Q<Button>("decrypt-panel__decode").clicked += OnDecode;
            view.Q<Button>("decrypt-panel__encode").clicked += OnEncode;
            view.Q<Button>("decrypt-panel__load").clicked += OnLoad;
            view.Q<Button>("decrypt-panel__clear").clicked += OnClear;
            view.Q<Button>("decrypt-panel__copy").clicked += () => PanelUi.Copy(_output.value);
            view.Q<Button>("decrypt-panel__use-output").clicked += () => _input.value = _output.value;
            return view;
        }

        public void OnSelected()
        {
        }

        private void OnDecode()
        {
            if (!TryBuildKeys(out EncryptionKeys keys))
            {
                return;
            }

            DataFormat? forced = FormatValues[System.Math.Max(0, _format.index)];
            Show(DataCodec.Decode(_input.value, forced, keys));
        }

        private void OnEncode()
        {
            if (!TryBuildKeys(out EncryptionKeys keys))
            {
                return;
            }

            DataIOConfig config = _state.Config;
            Show(DataCodec.Encode(_input.value, config.format, config.encryptionMode, config.storeAsBase64, keys));
        }

        private void OnLoad()
        {
            string path = EditorUtility.OpenFilePanel("Load a data file", string.Empty, string.Empty);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            _input.value = DataCodec.FileToText(File.ReadAllBytes(path));
            PanelUi.SetText(_status, $"Loaded {Path.GetFileName(path)}. Binary files are shown as Base64.");
        }

        private void OnClear()
        {
            _input.value = string.Empty;
            _output.value = string.Empty;
            _status.text = string.Empty;
        }

        private void Show(CodecResult result)
        {
            _output.value = result.Ok ? result.Text : string.Empty;
            PanelUi.SetText(_status, result.Message, result.Ok ? null : "error");
        }

        // Keys pasted under "Use other keys" win over the stored ones. Nothing pasted there is saved.
        private bool TryBuildKeys(out EncryptionKeys keys)
        {
            EncryptionKeys stored = _state.Keys;
            keys = new EncryptionKeys { AesKey = stored.AesKey, RsaPublicKey = stored.RsaPublicKey, RsaPrivateKey = stored.RsaPrivateKey };

            if (!string.IsNullOrWhiteSpace(_aes.value))
            {
                keys.AesKey = EncryptionKeys.DecodeAesKey(_aes.value);
                if (!keys.HasAesKey)
                {
                    PanelUi.SetText(_status, "The AES key must be 32 bytes as Base64.", "error");
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(_private.value))
            {
                keys.RsaPrivateKey = EncryptionKeys.DecodeXml(_private.value);
            }

            if (!string.IsNullOrWhiteSpace(_public.value))
            {
                keys.RsaPublicKey = EncryptionKeys.DecodeXml(_public.value);
            }

            return true;
        }
    }
}
