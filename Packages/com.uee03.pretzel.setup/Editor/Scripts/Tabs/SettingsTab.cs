using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pretzel.Setup.Folders;
using Pretzel.Setup.Paths;
using Pretzel.Setup.Settings;
using Pretzel.Setup.Windows;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Setup.Tabs
{
    public sealed class SettingsTab : IPretzelTab
    {
        private static readonly (string Label, string Url)[] Credits =
        {
            ("Lazy-Jedi (the original toolkit)", "https://github.com/Lazy-Jedi/Lazy-Jedi"),
            ("BluMalice", "https://github.com/BLUDRAG"),
            ("Kenney Assets", "https://www.kenney.nl/assets")
        };

        private PretzelTabContext _context;
        private VisualElement _view;

        private DropdownField _defaultPreset;
        private TextField _settingsFolder;
        private TextField _moduleUrl;
        private TextField _moduleVersion;
        private ListView _sources;
        private TextField _resources;
        private Toggle _customTemp;
        private TextField _temp;
        private Button _tempBrowse;

        public string Title => "Settings";

        public int Order => 100;

        public VisualElement CreateView(PretzelTabContext context)
        {
            _context = context;
            _view = SetupAssets.Layout("SettingsTab").Instantiate();
            _view.style.flexGrow = 1f;

            _defaultPreset = _view.Q<DropdownField>("settings-tab__default-preset");
            _settingsFolder = _view.Q<TextField>("settings-tab__settings-folder");
            _moduleUrl = _view.Q<TextField>("settings-tab__module-url");
            _moduleVersion = _view.Q<TextField>("settings-tab__module-version");
            _sources = _view.Q<ListView>("settings-tab__sources");
            _resources = _view.Q<TextField>("settings-tab__resources");
            _customTemp = _view.Q<Toggle>("settings-tab__custom-temp");
            _temp = _view.Q<TextField>("settings-tab__temp");
            _tempBrowse = _view.Q<Button>("settings-tab__temp-browse");

            SetUpPresets();
            SetUpData();
            SetUpModuleSource();
            SetUpSources();
            SetUpUserPaths();
            SetUpAbout();

            _view.Q<Button>("settings-tab__reset-project").clicked += OnResetProject;
            _context.SettingsReset += Sync;

            Sync();
            return _view;
        }

        public void OnSelected() => Sync();

        // Fields are filled without raising change events, so syncing never writes anything back.
        private void Sync()
        {
            PretzelProjectSettings project = _context.Project;
            PretzelUserSettings user = _context.User;

            _defaultPreset.choices = project.folderPresets.Select(p => p.name).ToList();
            _defaultPreset.SetValueWithoutNotify(project.activeFolderPreset);

            _settingsFolder.SetValueWithoutNotify(project.settingsFolder);
            _moduleUrl.SetValueWithoutNotify(project.moduleSourceUrl);
            _moduleVersion.SetValueWithoutNotify(project.moduleSourceVersion);

            _sources.itemsSource = project.catalogSources;
            _sources.Rebuild();

            _resources.SetValueWithoutNotify(user.resourcesFolder);
            _customTemp.SetValueWithoutNotify(user.useCustomTempFolder);
            ShowTempFolder();
        }

        #region PRESETS

        private void SetUpPresets()
        {
            _defaultPreset.RegisterValueChangedCallback(evt =>
            {
                _context.Project.activeFolderPreset = evt.newValue;
                _context.MarkProjectDirty();
            });

            _view.Q<Button>("settings-tab__reset-presets").clicked += () =>
            {
                if (!EditorUtility.DisplayDialog("Reset presets", "Restore the Game, Tool, System and Custom folder presets to their defaults?", "Reset", "Cancel"))
                {
                    return;
                }

                _context.Project.folderPresets = FolderPreset.CreateDefaults();
                _context.Project.Normalize();
                _context.MarkProjectDirty();
                _context.RaiseSettingsReset();
            };
        }

        #endregion

        #region DATA

        private void SetUpModuleSource()
        {
            _moduleUrl.RegisterValueChangedCallback(evt =>
            {
                _context.Project.moduleSourceUrl = evt.newValue.Trim();
                _context.MarkProjectDirty();
            });
            _moduleVersion.RegisterValueChangedCallback(evt =>
            {
                _context.Project.moduleSourceVersion = evt.newValue.Trim();
                _context.MarkProjectDirty();
            });
        }

        private void SetUpData()
        {
            RegisterFolderField(_settingsFolder, value => _context.Project.settingsFolder = value, () => _context.Project.settingsFolder);

            _view.Q<Button>("settings-tab__open-data-format").clicked += OpenDataFormat;
            _view.Q<Button>("settings-tab__open-persistent").clicked += () => OpenFolder(Application.persistentDataPath);
        }

        // The window belongs to the Data package, so Setup only calls its menu item.
        private static void OpenDataFormat()
        {
            if (!EditorApplication.ExecuteMenuItem("Pretzel/Data Format"))
            {
                EditorUtility.DisplayDialog("Data Format", "The Data Format window comes with the Pretzel Data package. Install it from the Addons tab.", "OK");
            }
        }

        private void RegisterFolderField(TextField field, Action<string> set, Func<string> get)
        {
            field.RegisterValueChangedCallback(evt =>
            {
                string value = ProjectFolders.Normalize(evt.newValue);
                if (value.Length > 0 && !ProjectFolders.IsValid(value, out string error))
                {
                    Debug.LogWarning($"[Pretzel] {error}");
                    field.SetValueWithoutNotify(get());
                    return;
                }

                set(value);
                field.SetValueWithoutNotify(value);
                _context.MarkProjectDirty();
            });
        }

        #endregion

        #region CATALOG SOURCES

        private void SetUpSources()
        {
            _sources.fixedItemHeight = 24f;
            _sources.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            _sources.reorderable = true;
            _sources.reorderMode = ListViewReorderMode.Animated;
            _sources.showBorder = true;
            _sources.showAddRemoveFooter = true;
            _sources.showBoundCollectionSize = false;
            _sources.selectionType = SelectionType.Single;

            _sources.makeItem = () =>
            {
                TextField field = new();
                field.AddToClassList("pretzel-folder-list__item");
                field.RegisterValueChangedCallback(evt =>
                {
                    List<string> sources = _context.Project.catalogSources;
                    if (field.userData is int index && index >= 0 && index < sources.Count)
                    {
                        sources[index] = evt.newValue.Trim();
                        _context.MarkProjectDirty();
                    }
                });
                return field;
            };
            _sources.bindItem = (element, index) =>
            {
                TextField field = (TextField)element;
                field.userData = index;
                field.SetValueWithoutNotify(_context.Project.catalogSources[index] ?? string.Empty);
            };
            _sources.itemsAdded += indices =>
            {
                foreach (int index in indices)
                {
                    _context.Project.catalogSources[index] = string.Empty;
                }

                _context.MarkProjectDirty();
            };
            _sources.itemsRemoved += _ => _context.MarkProjectDirty();
            _sources.itemIndexChanged += (_, _) => _context.MarkProjectDirty();
        }

        #endregion

        #region USER PATHS

        private void SetUpUserPaths()
        {
            _resources.RegisterValueChangedCallback(evt =>
            {
                _context.User.resourcesFolder = evt.newValue.Trim();
                _context.MarkUserDirty();
            });
            _view.Q<Button>("settings-tab__resources-browse").clicked += () =>
            {
                string path = BrowseFolder(_context.User.resourcesFolder);
                if (path != null)
                {
                    _resources.value = path;
                }
            };

            _customTemp.RegisterValueChangedCallback(evt =>
            {
                _context.User.useCustomTempFolder = evt.newValue;
                _context.MarkUserDirty();
                ShowTempFolder();
            });
            _temp.RegisterValueChangedCallback(evt =>
            {
                if (_context.User.useCustomTempFolder)
                {
                    _context.User.tempFolder = evt.newValue.Trim();
                    _context.MarkUserDirty();
                }
            });
            _tempBrowse.clicked += () =>
            {
                string path = BrowseFolder(_context.User.tempFolder);
                if (path != null)
                {
                    _temp.value = path;
                }
            };

            _view.Q<Button>("settings-tab__reset-user").clicked += () =>
            {
                PretzelSettings.ResetUser();
                _context.RaiseSettingsReset();
            };
        }

        private void ShowTempFolder()
        {
            bool custom = _context.User.useCustomTempFolder;
            _temp.SetEnabled(custom);
            _tempBrowse.SetEnabled(custom);
            _temp.SetValueWithoutNotify(custom ? _context.User.tempFolder : PretzelPaths.SystemTempFolder);
        }

        private static string BrowseFolder(string current)
        {
            string start = !string.IsNullOrEmpty(current) && Directory.Exists(current) ? current : PretzelPaths.ProjectFolder;
            string path = EditorUtility.OpenFolderPanel("Select Folder", start, string.Empty);
            return string.IsNullOrEmpty(path) ? null : path;
        }

        #endregion

        #region ABOUT

        private void SetUpAbout()
        {
            _view.Q<Label>("settings-tab__about-text").text =
                $"Pretzel {SetupAssets.PackageVersion()}, a modular toolkit by Ubaidullah Effendi. It grew out of Lazy-Jedi.";

            VisualElement links = _view.Q<VisualElement>("settings-tab__about-links");
            foreach ((string label, string url) in Credits)
            {
                Button link = new(() => Application.OpenURL(url)) { text = label };
                link.AddToClassList("pretzel-about__link");
                links.Add(link);
            }
        }

        #endregion

        private void OnResetProject()
        {
            if (!EditorUtility.DisplayDialog("Reset project settings", "Restore every project-wide Pretzel setting to its default?", "Reset", "Cancel"))
            {
                return;
            }

            PretzelSettings.ResetProject();
            _context.RaiseSettingsReset();
        }

        private static void OpenFolder(string folder)
        {
            if (!PretzelPaths.Open(folder))
            {
                EditorUtility.DisplayDialog("Folder not found", $"'{folder}' does not exist yet.", "OK");
            }
        }
    }
}
