using System.Collections.Generic;
using System.Linq;
using Pretzel.Setup.Folders;
using Pretzel.Setup.Settings;
using Pretzel.Setup.Tabs;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Samples.Setup
{
    /// <summary>
    /// A tab in Pretzel > Project Setup that checks the folders of the active preset and creates the missing ones.
    /// The window finds it with TypeCache, so the class only needs a parameterless constructor.
    /// </summary>
    public sealed class FolderChecklistTab : IPretzelTab
    {
        private PretzelTabContext _context;
        private DropdownField _presetField;
        private VisualElement _folderList;
        private Button _createButton;

        public string Title => "Folder Checklist";

        // Between Addons Extra (20) and Settings (100).
        public int Order => 50;

        public VisualElement CreateView(PretzelTabContext context)
        {
            _context = context;

            ScrollView root = new();
            root.AddToClassList("pretzel-tab");

            VisualElement section = new();
            section.AddToClassList("pretzel-section");
            root.Add(section);

            Label title = new("Folder checklist");
            title.AddToClassList("pretzel-section__title");
            section.Add(title);

            Label hint = new("Shows which folders of the active preset exist under Assets. Changing the preset here changes it for the whole window.");
            hint.AddToClassList("pretzel-section__hint");
            section.Add(hint);

            _presetField = new DropdownField("Folder preset");
            _presetField.AddToClassList("pretzel-section__row");
            _presetField.RegisterValueChangedCallback(OnPresetChanged);
            section.Add(_presetField);

            _folderList = new VisualElement();
            section.Add(_folderList);

            _createButton = new Button(CreateMissingFolders);
            _createButton.AddToClassList("pretzel-section__action");
            _createButton.AddToClassList("pretzel-section__action--primary");
            section.Add(_createButton);

            // The context outlives this view, so subscribe only while the view is on screen.
            root.RegisterCallback<AttachToPanelEvent>(_ => _context.SettingsReset += Refresh);
            root.RegisterCallback<DetachFromPanelEvent>(_ => _context.SettingsReset -= Refresh);

            Refresh();
            return root;
        }

        // Folders can be created or deleted outside the window, so refresh each time the tab is shown.
        public void OnSelected() => Refresh();

        private void Refresh()
        {
            PretzelProjectSettings project = _context.Project;
            _presetField.choices = project.folderPresets.Select(preset => preset.name).ToList();
            _presetField.SetValueWithoutNotify(project.ActivePreset.name);
            RefreshFolders();
        }

        private void OnPresetChanged(ChangeEvent<string> evt)
        {
            _context.Project.activeFolderPreset = evt.newValue;

            // Marking the settings dirty is what makes the window save them.
            _context.MarkProjectDirty();
            RefreshFolders();
        }

        private void RefreshFolders()
        {
            _folderList.Clear();
            int missing = 0;
            foreach (string folder in _context.Project.ActivePreset.folders)
            {
                string text;
                if (!ProjectFolders.IsValid(folder, out string error))
                {
                    text = $"Invalid  {folder}  ({error})";
                }
                else if (ProjectFolders.Exists(folder))
                {
                    text = "Exists   " + ProjectFolders.Normalize(folder);
                }
                else
                {
                    text = "Missing  " + ProjectFolders.Normalize(folder);
                    missing++;
                }

                Label row = new(text);
                row.AddToClassList("pretzel-section__hint");
                _folderList.Add(row);
            }

            _createButton.text = missing > 0 ? $"Create {missing} missing folder(s)" : "All folders exist";
            _createButton.SetEnabled(missing > 0);
        }

        private void CreateMissingFolders()
        {
            List<string> folders = _context.Project.ActivePreset.folders;
            int created = ProjectFolders.Create(folders);
            Debug.Log($"[Pretzel Samples] Created {created} folder(s) for the '{_context.Project.ActivePreset.name}' preset.");
            RefreshFolders();
        }
    }
}
