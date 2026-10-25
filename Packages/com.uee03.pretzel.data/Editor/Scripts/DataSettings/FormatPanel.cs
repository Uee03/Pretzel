using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Data.Editor
{
    internal sealed class FormatPanel : IDataFormatPanel
    {
        private static readonly DataFormat[] Formats =
        {
            DataFormat.PlainText, DataFormat.Binary, DataFormat.Encrypted
        };

        private readonly List<Button> _buttons = new();
        private DataFormatState _state;
        private DataFormat? _shown;

        private Label _title;
        private Label _summary;
        private VisualElement _pros;
        private VisualElement _cons;
        private Label _best;
        private DropdownField _extension;
        private EnumField _serializer;
        private TextField _folder;
        private Toggle _pretty;
        private Toggle _backup;
        private Toggle _base64;
        private Label _hint;
        private HelpBox _warning;

        public string Title => "Format";

        public VisualElement CreateView(DataFormatState state)
        {
            _state = state;
            VisualElement view = DataEditorAssets.Layout("DataFormatTab").Instantiate();
            view.style.flexGrow = 1f;

            _title = view.Q<Label>("format-panel__title");
            _summary = view.Q<Label>("format-panel__summary");
            _pros = view.Q("format-panel__pros");
            _cons = view.Q("format-panel__cons");
            _best = view.Q<Label>("format-panel__best");
            _extension = view.Q<DropdownField>("format-panel__extension");
            _serializer = view.Q<EnumField>("format-panel__serializer");
            _folder = view.Q<TextField>("format-panel__folder");
            _pretty = view.Q<Toggle>("format-panel__pretty");
            _backup = view.Q<Toggle>("format-panel__backup");
            _base64 = view.Q<Toggle>("format-panel__base64");
            _hint = view.Q<Label>("format-panel__hint");
            _warning = view.Q<HelpBox>("format-panel__warning");

            VisualElement picker = view.Q("format-panel__picker");
            foreach (DataFormat format in Formats)
            {
                DataFormat chosen = format;
                Button button = new(() => Select(chosen))
                {
                    text = DataFormatInfo.Get(format).Title + "\n." + DataFormats.DefaultExtension(format)
                };
                button.AddToClassList("pretzel-format-picker__option");
                _buttons.Add(button);
                picker.Add(button);
            }

            PanelUi.FillBullets(view.Q("format-panel__base64-pros"), DataFormatInfo.Base64Pros);
            PanelUi.FillBullets(view.Q("format-panel__base64-cons"), DataFormatInfo.Base64Cons);

            _serializer.Init(SerializerChoice.Newtonsoft);
            _extension.RegisterValueChangedCallback(evt => _state.Edit("Change file extension", c => c.fileExtension = evt.newValue));
            _serializer.RegisterValueChangedCallback(evt => _state.Edit("Change serializer", c => c.serializer = (SerializerChoice)evt.newValue));
            _pretty.RegisterValueChangedCallback(evt => _state.Edit("Change pretty print", c => c.prettyPrint = evt.newValue));
            _backup.RegisterValueChangedCallback(evt => _state.Edit("Change backup", c => c.keepBackup = evt.newValue));
            _base64.RegisterValueChangedCallback(evt => _state.Edit("Change Base64", c => c.storeAsBase64 = evt.newValue));
            _folder.RegisterValueChangedCallback(evt => OnFolderChanged(evt.newValue));

            _state.Changed += Refresh;
            Refresh();
            return view;
        }

        public void OnSelected() => Refresh();

        private void Select(DataFormat format)
        {
            _state.Edit("Change data format", c =>
            {
                c.format = format;
                c.fileExtension = DataFormats.DefaultExtension(format);
            });
        }

        private void OnFolderChanged(string value)
        {
            string folder = SaveFolder.Normalize(value);
            if (folder.Length > 0 && !SaveFolder.IsValid(folder, out string error))
            {
                Debug.LogWarning($"[Pretzel] {error}");
                _folder.SetValueWithoutNotify(_state.Config.saveFolder);
                return;
            }

            _folder.SetValueWithoutNotify(folder);
            _state.Edit("Change save folder", c => c.saveFolder = folder);
        }

        // Fields are filled without raising change events, so refreshing never writes anything back.
        private void Refresh()
        {
            DataIOConfig config = _state.Config;
            if (!config)
            {
                return;
            }

            for (int i = 0; i < Formats.Length; i++)
            {
                _buttons[i].EnableInClassList("pretzel-format-picker__option--selected", Formats[i] == config.format);
            }

            if (_shown != config.format)
            {
                _shown = config.format;
                FormatInfo info = DataFormatInfo.Get(config.format);
                _title.text = info.Title;
                _summary.text = info.Summary;
                PanelUi.FillBullets(_pros, info.Pros);
                PanelUi.FillBullets(_cons, info.Cons);
                _best.text = info.BestFor;

                List<string> choices = new(DataFormatInfo.Get(config.format).Extensions);
                _extension.choices = choices;
            }

            if (!_extension.choices.Contains(config.fileExtension))
            {
                _extension.choices = new List<string>(_extension.choices) { config.fileExtension };
            }

            _extension.SetValueWithoutNotify(config.fileExtension);
            _serializer.SetValueWithoutNotify(config.serializer);
            _folder.SetValueWithoutNotify(config.saveFolder);
            _pretty.SetValueWithoutNotify(config.prettyPrint);
            _backup.SetValueWithoutNotify(config.keepBackup);
            _base64.SetValueWithoutNotify(config.storeAsBase64);

            bool plain = config.format == DataFormat.PlainText;
            _pretty.SetEnabled(plain);
            _base64.SetEnabled(!plain);
            _hint.text = plain
                ? "Pretty print indents the JSON. Base64 does not apply to plain text."
                : "Pretty print only applies to plain text. Base64 writes the bytes as text, about 33 percent bigger.";

            ShowWarning();
        }

        private void ShowWarning()
        {
            DataIOConfig config = _state.Config;
            string message = null;
            HelpBoxMessageType type = HelpBoxMessageType.Warning;

            if (config.format == DataFormat.Encrypted)
            {
                EncryptionKeys keys = _state.Keys;
                if (config.encryptionMode == EncryptionMode.Aes && !keys.HasAesKey)
                {
                    message = "Encrypted is selected, but there is no AES key yet. Generate one on the Encryption tab.";
                }
                else if (config.encryptionMode != EncryptionMode.Aes)
                {
                    message = "Unsupported saved encryption mode. Set up AES again. This setting has not been changed.";
                    type = HelpBoxMessageType.Error;
                }
            }

            _warning.messageType = type;
            _warning.text = message ?? string.Empty;
            PanelUi.SetVisible(_warning, message != null);
        }
    }
}
