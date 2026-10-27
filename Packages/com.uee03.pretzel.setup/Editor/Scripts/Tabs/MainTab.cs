using System;
using System.Collections.Generic;
using System.Linq;
using Pretzel.Setup.Folders;
using Pretzel.Setup.Settings;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.SceneTemplate;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Setup.Tabs
{
    public sealed class MainTab : IPretzelTab
    {
        private const string TemplatesPackagePath = "Packages/" + SetupAssets.PackageId + "/SceneTemplates/";

        private enum CheckState
        {
            Done,
            Partial,
            Todo
        }

        private sealed class ChecklistRow
        {
            public Label Chip;
            public Func<CheckState> Evaluate;
        }

        private PretzelTabContext _context;
        private VisualElement _view;

        private TextField _company;
        private TextField _product;
        private TextField _version;
        private ObjectField _icon;
        private ObjectField _cursor;

        private DropdownField _preset;
        private ListView _folders;
        private Label _folderStatus;
        private VisualElement _sceneTemplates;
        private readonly List<ChecklistRow> _checklist = new();

        public string Title => "Main";

        public int Order => 0;

        public VisualElement CreateView(PretzelTabContext context)
        {
            _context = context;
            _view = SetupAssets.Layout("MainTab").Instantiate();
            _view.style.flexGrow = 1f;

            _company = _view.Q<TextField>("main-tab__company");
            _product = _view.Q<TextField>("main-tab__product");
            _version = _view.Q<TextField>("main-tab__version");
            _icon = _view.Q<ObjectField>("main-tab__icon");
            _cursor = _view.Q<ObjectField>("main-tab__cursor");
            _preset = _view.Q<DropdownField>("main-tab__preset");
            _folders = _view.Q<ListView>("main-tab__folders");
            _folderStatus = _view.Q<Label>("main-tab__folder-status");
            _sceneTemplates = _view.Q<VisualElement>("main-tab__scene-templates");

            SetUpProjectFields();
            SetUpFolders();
            BuildChecklist(_view.Q<VisualElement>("main-tab__checklist"));
            AddContributedSections();

            _view.Q<Button>("main-tab__create-folders").clicked += OnCreateFolders;
            _context.SettingsReset += OnSettingsReset;

            OnSelected();
            return _view;
        }

        public void OnSelected()
        {
            SyncProjectFields();
            SyncPreset();
            BuildSceneTemplates();
            RefreshChecklist();
        }

        #region PROJECT

        private void AddContributedSections()
        {
            List<IPretzelMainSection> sections = new();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<IPretzelMainSection>())
            {
                if (!type.IsAbstract && !type.IsInterface && type.GetConstructor(Type.EmptyTypes) != null)
                {
                    sections.Add((IPretzelMainSection)Activator.CreateInstance(type));
                }
            }

            sections.Sort((a, b) => a.Order.CompareTo(b.Order));

            ScrollView scroll = _view.Q<ScrollView>();
            int index = 0;
            foreach (IPretzelMainSection section in sections)
            {
                VisualElement element = section.CreateSection(_context);
                if (element != null)
                {
                    scroll.Insert(index++, element);
                }
            }
        }

        private void SetUpProjectFields()
        {
            _company.isDelayed = _product.isDelayed = _version.isDelayed = true;

            _company.RegisterValueChangedCallback(evt =>
            {
                PlayerSettings.companyName = evt.newValue;
                RefreshChecklist();
            });
            _product.RegisterValueChangedCallback(evt =>
            {
                PlayerSettings.productName = evt.newValue;
                RefreshChecklist();
            });
            _version.RegisterValueChangedCallback(evt => PlayerSettings.bundleVersion = evt.newValue);

            _icon.objectType = typeof(Texture2D);
            _icon.allowSceneObjects = false;
            _icon.RegisterValueChangedCallback(evt =>
            {
                // NamedBuildTarget.Unknown is the default icon, used by every platform without its own.
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { evt.newValue as Texture2D }, IconKind.Application);
                RefreshChecklist();
            });

            _cursor.objectType = typeof(Texture2D);
            _cursor.allowSceneObjects = false;
            _cursor.RegisterValueChangedCallback(evt => PlayerSettings.defaultCursor = evt.newValue as Texture2D);
        }

        private void SyncProjectFields()
        {
            _company.SetValueWithoutNotify(PlayerSettings.companyName);
            _product.SetValueWithoutNotify(PlayerSettings.productName);
            _version.SetValueWithoutNotify(PlayerSettings.bundleVersion);
            _icon.SetValueWithoutNotify(PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Application).FirstOrDefault());
            _cursor.SetValueWithoutNotify(PlayerSettings.defaultCursor);
        }

        #endregion

        #region FOLDERS

        private void SetUpFolders()
        {
            _folders.fixedItemHeight = 24f;
            _folders.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            _folders.reorderable = true;
            _folders.reorderMode = ListViewReorderMode.Animated;
            _folders.showBorder = true;
            _folders.showAddRemoveFooter = true;
            _folders.showBoundCollectionSize = false;
            _folders.selectionType = SelectionType.Single;
            _folders.makeItem = MakeFolderItem;
            _folders.bindItem = BindFolderItem;
            _folders.itemsAdded += OnFoldersAdded;
            _folders.itemsRemoved += _ => OnFoldersChanged();
            _folders.itemIndexChanged += (_, _) => OnFoldersChanged();

            _preset.RegisterValueChangedCallback(evt =>
            {
                _context.Project.activeFolderPreset = evt.newValue;
                _context.MarkProjectDirty();
                BindFolderList();
            });
        }

        private void SyncPreset()
        {
            PretzelProjectSettings project = _context.Project;
            _preset.choices = project.folderPresets.Select(p => p.name).ToList();
            _preset.SetValueWithoutNotify(project.activeFolderPreset);
            BindFolderList();
        }

        private void BindFolderList()
        {
            _folders.itemsSource = _context.Project.ActivePreset.folders;
            _folders.Rebuild();
            RefreshFolderStatus();
        }

        // Callbacks are registered once here. bindItem only updates the index, so reordering never leaves a stale one.
        private VisualElement MakeFolderItem()
        {
            TextField field = new();
            field.AddToClassList("pretzel-folder-list__item");
            field.RegisterValueChangedCallback(evt =>
            {
                List<string> folders = _context.Project.ActivePreset.folders;
                if (field.userData is int index && index >= 0 && index < folders.Count)
                {
                    folders[index] = evt.newValue;
                    OnFoldersChanged();
                }
            });
            return field;
        }

        private void BindFolderItem(VisualElement element, int index)
        {
            TextField field = (TextField)element;
            field.userData = index;
            field.SetValueWithoutNotify(_context.Project.ActivePreset.folders[index] ?? string.Empty);
        }

        private void OnFoldersAdded(IEnumerable<int> indices)
        {
            List<string> folders = _context.Project.ActivePreset.folders;
            foreach (int index in indices)
            {
                folders[index] = string.Empty;
            }

            OnFoldersChanged();
        }

        private void OnFoldersChanged()
        {
            _context.MarkProjectDirty();
            RefreshFolderStatus();
            RefreshChecklist();
        }

        private void RefreshFolderStatus()
        {
            List<string> folders = _context.Project.ActivePreset.folders;
            int existing = folders.Count(ProjectFolders.Exists);
            _folderStatus.text = $"{existing} of {folders.Count} folders exist.";
        }

        private void OnCreateFolders()
        {
            int created = ProjectFolders.Create(_context.Project.ActivePreset.folders);
            AssetDatabase.SaveAssets();
            RefreshFolderStatus();
            RefreshChecklist();
            _folderStatus.text += created == 0 ? " Nothing to create." : $" Created {created}.";
        }

        #endregion

        #region SCENES

        private void BuildSceneTemplates()
        {
            _sceneTemplates.Clear();

            List<SceneTemplateAsset> templates = AssetDatabase.FindAssets("t:SceneTemplateAsset")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.StartsWith(TemplatesPackagePath, StringComparison.Ordinal))
                .Select(AssetDatabase.LoadAssetAtPath<SceneTemplateAsset>)
                .Where(template => template)
                .ToList();

            if (templates.Count == 0)
            {
                _sceneTemplates.Add(new HelpBox("No Pretzel scene templates found. They come with the Pretzel Templates package.", HelpBoxMessageType.Info));
                return;
            }

            foreach (SceneTemplateAsset template in templates)
            {
                Button button = new(() => NewSceneFrom(template)) { text = "New " + template.name };
                button.AddToClassList("pretzel-scene-templates__button");
                _sceneTemplates.Add(button);
            }
        }

        private static void NewSceneFrom(SceneTemplateAsset template)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                SceneTemplateService.Instantiate(template, false);
            }
        }

        #endregion

        #region CHECKLIST

        private void BuildChecklist(VisualElement container)
        {
            AddCheck(container, "Company name set", () => IsSet(PlayerSettings.companyName, "DefaultCompany"));
            AddCheck(container, "Product name set", () => IsSet(PlayerSettings.productName, null));
            AddCheck(container, "Application icon set", () =>
                PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Application).Any(icon => icon) ? CheckState.Done : CheckState.Todo);
            AddCheck(container, "Folder structure created", () =>
            {
                List<string> folders = _context.Project.ActivePreset.folders;
                int existing = folders.Count(ProjectFolders.Exists);
                return existing == folders.Count ? CheckState.Done : existing == 0 ? CheckState.Todo : CheckState.Partial;
            });
            AddCheck(container, "Scenes in Build Settings", () => EditorBuildSettings.scenes.Length > 0 ? CheckState.Done : CheckState.Todo);
        }

        private void AddCheck(VisualElement container, string label, Func<CheckState> evaluate)
        {
            VisualElement row = new();
            row.AddToClassList("pretzel-checklist__item");
            row.Add(new Label(label));

            Label chip = new();
            chip.AddToClassList("pretzel-chip");
            row.Add(chip);

            container.Add(row);
            _checklist.Add(new ChecklistRow { Chip = chip, Evaluate = evaluate });
        }

        private void RefreshChecklist()
        {
            foreach (ChecklistRow row in _checklist)
            {
                CheckState state = row.Evaluate();
                row.Chip.text = state switch
                {
                    CheckState.Done => "Done",
                    CheckState.Partial => "Partial",
                    _ => "To do"
                };
                row.Chip.EnableInClassList("pretzel-chip--ok", state == CheckState.Done);
                row.Chip.EnableInClassList("pretzel-chip--warn", state == CheckState.Partial);
                row.Chip.EnableInClassList("pretzel-chip--missing", state == CheckState.Todo);
            }
        }

        private static CheckState IsSet(string value, string placeholder)
        {
            return string.IsNullOrWhiteSpace(value) || value == placeholder ? CheckState.Todo : CheckState.Done;
        }

        #endregion

        private void OnSettingsReset()
        {
            SyncPreset();
            RefreshChecklist();
        }
    }
}
