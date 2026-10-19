using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Data.Editor
{
    internal sealed class ShippingPanel : IDataFormatPanel
    {
        private DataFormatState _state;
        private HelpBox _formatWarning;
        private DropdownField _delivery;
        private Label _deliveryHint;
        private TextField _folder;
        private HelpBox _embedWarning;
        private Button _create;
        private Label _result;
        private KeyDelivery[] _options = System.Array.Empty<KeyDelivery>();
        private EncryptionMode? _optionsFor;

        public string Title => "Shipping";

        public VisualElement CreateView(DataFormatState state)
        {
            _state = state;
            VisualElement view = DataEditorAssets.Layout("ShippingTab").Instantiate();
            view.style.flexGrow = 1f;

            _formatWarning = view.Q<HelpBox>("shipping-panel__format-warning");
            _delivery = view.Q<DropdownField>("shipping-panel__delivery");
            _deliveryHint = view.Q<Label>("shipping-panel__delivery-hint");
            _folder = view.Q<TextField>("shipping-panel__folder");
            _embedWarning = view.Q<HelpBox>("shipping-panel__embed-warning");
            _create = view.Q<Button>("shipping-panel__create");
            _result = view.Q<Label>("shipping-panel__result");

            _folder.SetValueWithoutNotify(ShippingGenerator.DefaultFolder);
            PanelUi.FillBullets(view.Q("shipping-panel__token-steps"), DataFormatInfo.TokenSteps, true);

            _delivery.RegisterValueChangedCallback(_ => Refresh());
            _create.clicked += OnCreate;
            view.Q<Button>("shipping-panel__guide").clicked += OnOpenGuide;

            _state.Changed += Refresh;
            Refresh();
            return view;
        }

        public void OnSelected() => Refresh();

        private KeyDelivery Selected => _options[Mathf.Clamp(_delivery.index, 0, _options.Length - 1)];

        private void Refresh()
        {
            DataIOConfig config = _state.Config;
            if (!config)
            {
                return;
            }

            if (_optionsFor != config.encryptionMode)
            {
                _optionsFor = config.encryptionMode;
                if (config.encryptionMode == EncryptionMode.Aes)
                {
                    _options = new[] { KeyDelivery.Environment, KeyDelivery.Embedded, KeyDelivery.Token };
                    _delivery.choices = new List<string> { "Environment variables on the machine", "Embedded in the game (obfuscated)", "Fetch with a token at startup" };
                }
                else
                {
                    _options = new[] { KeyDelivery.Environment, KeyDelivery.Embedded };
                    _delivery.choices = new List<string> { "Environment variables on the machine", "Embed the public key (safe)" };
                }

                _delivery.SetValueWithoutNotify(_delivery.choices[0]);
            }

            bool encrypted = config.format == DataFormat.Encrypted;
            _formatWarning.text = "Shipping keys only matters for the Encrypted format. Choose it on the Format tab.";
            PanelUi.SetVisible(_formatWarning, !encrypted);
            _create.SetEnabled(encrypted);

            KeyDelivery delivery = Selected;
            bool rsa = config.encryptionMode == EncryptionMode.Rsa;
            _deliveryHint.text = delivery switch
            {
                KeyDelivery.Environment => $"The game reads {config.environmentPrefix}_* at startup and logs an error when they are missing. Good for servers, tools and testing. Players cannot be expected to set variables.",
                KeyDelivery.Embedded when rsa => "Only the public key is written into the script. That is safe: it can encrypt but not decrypt. The private key is never written, and never ship it.",
                KeyDelivery.Embedded => "The AES key is written into the script as two XOR-masked arrays and registered before the first scene loads. This only stops a plain search of the build. A determined player can still extract it.",
                _ => "The game contains no key. After sign-in it sends an access token to your server, which returns the key. The script has a stub for your own request."
            };

            bool warn = delivery == KeyDelivery.Embedded && !rsa;
            _embedWarning.text = "The generated script contains the AES key. Do not commit it to a public repository. Add the output folder to .gitignore.";
            PanelUi.SetVisible(_embedWarning, warn);
        }

        private void OnCreate()
        {
            string folder = _folder.value.Trim().Replace('\\', '/').TrimEnd('/');
            string error;
            string path;
            try
            {
                error = ShippingGenerator.Generate(folder, Selected, _state.Config, _state.Keys, out path);
            }
            catch (System.Exception ex) when (ex is IOException or System.UnauthorizedAccessException)
            {
                PanelUi.SetText(_result, $"Could not generate shipping files: {PanelUi.ErrorText(ex)}", "error");
                return;
            }

            if (error != null)
            {
                PanelUi.SetText(_result, error, "error");
                return;
            }

            PanelUi.SetText(_result, $"Created {path}, {ShippingGenerator.GuideName}, and {ShippingGenerator.ExampleName} "
                + "(existing example edits preserved). Add the example to a GameObject and use its context menu in Play mode.");
            Object script = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (script)
            {
                EditorGUIUtility.PingObject(script);
            }
        }

        private void OnOpenGuide()
        {
            string path = ShippingGenerator.GuideSourcePath();
            if (path != null && File.Exists(path))
            {
                EditorUtility.OpenWithDefaultApp(path);
            }
            else
            {
                PanelUi.SetText(_result, "The guide was not found in the package.", "error");
            }
        }
    }
}
