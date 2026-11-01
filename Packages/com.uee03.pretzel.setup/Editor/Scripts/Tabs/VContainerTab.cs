using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pretzel.Setup.Addons.Catalog;
using Pretzel.Setup.Addons.Services;
using Pretzel.Setup.Composition;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Setup.Tabs
{
    /// <summary>Sets up VContainer: a root scope created before the first scene, an installer, and generated service code.</summary>
    public sealed class VContainerTab : IPretzelTab
    {
        private const string GuidePath = "Packages/com.uee03.pretzel.vcontainer/Documentation~/VContainer.md";
        private const string NoContainerGuidePath = "Packages/com.uee03.pretzel.setup/Documentation~/WithoutAContainer.md";
        private const string DiagnosticsMenu = "Window/VContainer Diagnostics";
        private const string OfficialDocsUrl = "https://vcontainer.hadashikick.jp/";

        private PretzelTabContext _context;
        private Label _vcontainerStatus;
        private Label _integrationStatus;
        private Label _rootStatus;
        private Button _install;
        private HelpBox _assemblyWarning;
        private TextField _apiUrl;
        private VisualElement _serviceRows;
        private Label _result;
        private Toggle _starter;
        private Toggle _starterExample;
        private Label _starterHint;
        private Toggle _diagnostics;
        private HelpBox _diagnosticsWarning;
        private readonly Dictionary<string, Toggle> _pretzelToggles = new(StringComparer.OrdinalIgnoreCase);

        public string Title => "VContainer";

        public int Order => 60;

        private CompositionSettings Settings => _context.Project.composition;

        public VisualElement CreateView(PretzelTabContext context)
        {
            _context = context;
            ScrollView view = new();
            view.AddToClassList("pretzel-tab");
            view.Add(CreateStatusSection());
            view.Add(CreateProjectSection());
            view.Add(CreateStarterSection());
            view.Add(CreatePretzelSection());
            view.Add(CreateServicesSection());
            view.Add(CreateActionsSection());
            view.Add(CreateDiagnosticsSection());

            _context.SettingsReset += Rebuild;
            view.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                CompositionAfterCompile.Applied += ShowLog;
                PackageService.Changed += Refresh;
            });
            view.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                CompositionAfterCompile.Applied -= ShowLog;
                PackageService.Changed -= Refresh;
            });
            Refresh();
            return view;
        }

        public void OnSelected() => Refresh();

        #region SECTIONS

        private VisualElement CreateStatusSection()
        {
            VisualElement section = Section("Status");
            _vcontainerStatus = Hint(section);
            _integrationStatus = Hint(section);
            _rootStatus = Hint(section);

            _install = Action(section, "Install VContainer", InstallVContainer, primary: true);
            Action(section, "Open the VContainer guide", () => OpenGuide(GuidePath));
            Action(section, "Open the guide for working without a container", () => OpenGuide(NoContainerGuidePath));
            Action(section, "Open the official VContainer documentation", () => Application.OpenURL(OfficialDocsUrl));
            return section;
        }

        private VisualElement CreateProjectSection()
        {
            VisualElement section = Section("Composition root");
            Hint(section).text = "One root scope prefab, created by VContainer before the first scene loads and kept for the whole session. " +
                                 "It is an alternative to a GameManager: services are plain classes registered in the installer, and the flow class is the entry point. " +
                                 "A container is optional. The no-container guide shows the same setup with ScriptableObjects.";

            Bind(section, "Namespace", () => Settings.namespaceName, v => Settings.namespaceName = v);
            Bind(section, "Scope name", () => Settings.scopeName, v => Settings.scopeName = v);
            Bind(section, "Code folder", () => Settings.codeFolder, v => Settings.codeFolder = v);
            Bind(section, "Asset folder", () => Settings.assetFolder, v => Settings.assetFolder = v);

            Toggle flow = new("Create a flow entry point") { value = Settings.createFlow };
            flow.RegisterValueChangedCallback(evt =>
            {
                Settings.createFlow = evt.newValue;
                _context.MarkProjectDirty();
            });
            section.Add(flow);

            _assemblyWarning = new HelpBox(string.Empty, HelpBoxMessageType.Warning);
            section.Add(_assemblyWarning);
            return section;
        }

        private VisualElement CreateStarterSection()
        {
            VisualElement section = Section("Starter services");
            _starterHint = Hint(section);

            _starter = new Toggle("SaveService and GameService");
            _starter.RegisterValueChangedCallback(evt =>
            {
                Settings.starterServices = evt.newValue;
                _context.MarkProjectDirty();
                Refresh();
            });
            section.Add(_starter);

            _starterExample = new Toggle("Usage example (GameServicesExample)");
            _starterExample.RegisterValueChangedCallback(evt =>
            {
                Settings.starterExample = evt.newValue;
                _context.MarkProjectDirty();
            });
            section.Add(_starterExample);
            return section;
        }

        private VisualElement CreatePretzelSection()
        {
            VisualElement section = Section("Pretzel services");
            Hint(section).text = "Registered as singletons in the root scope. Modules that are not installed are skipped.";
            foreach (PretzelServiceOption option in PretzelServiceCatalog.All)
            {
                Toggle toggle = new(option.Label);
                toggle.RegisterValueChangedCallback(evt =>
                {
                    Settings.pretzelServices.RemoveAll(id => string.Equals(id, option.Id, StringComparison.OrdinalIgnoreCase));
                    if (evt.newValue)
                    {
                        Settings.pretzelServices.Add(option.Id);
                    }

                    _context.MarkProjectDirty();
                    ShowApiUrl();
                });
                _pretzelToggles[option.Id] = toggle;
                section.Add(toggle);
            }

            _apiUrl = Bind(section, "API base URL", () => Settings.apiBaseUrl, v => Settings.apiBaseUrl = v);
            return section;
        }

        private VisualElement CreateServicesSection()
        {
            VisualElement section = Section("Services and managers");
            Hint(section).text = "Each entry is registered by the installer. Missing files are created with an interface and the chosen entry points; existing files are never changed.";
            _serviceRows = new VisualElement();
            section.Add(_serviceRows);
            Action(section, "Add service", () =>
            {
                Settings.services.Add(new ServiceDefinition { name = UniqueServiceName() });
                _context.MarkProjectDirty();
                RebuildServices();
            });
            return section;
        }

        private VisualElement CreateActionsSection()
        {
            VisualElement section = Section("Generate");
            Action(section, "Generate and set up", GenerateAndSetUp, primary: true);
            Action(section, "Add a scope to the open scene", AddSceneScope);
            _result = Hint(section);
            return section;
        }

        private VisualElement CreateDiagnosticsSection()
        {
            VisualElement section = Section("Diagnostics");
            Hint(section).text = "Records every scope, registration and resolve for the VContainer Diagnostics window. " +
                                 "Collection also runs in player builds and costs performance, so turn it off before a release build.";

            _diagnostics = new Toggle("Enable diagnostics");
            _diagnostics.RegisterValueChangedCallback(evt =>
            {
                UnityEngine.Object settings = CompositionAssets.PreloadedSettings();
                if (settings != null)
                {
                    CompositionAssets.SetDiagnostics(settings, evt.newValue);
                }

                Refresh();
            });
            section.Add(_diagnostics);

            _diagnosticsWarning = new HelpBox("Diagnostics are on. Player builds will collect them too.", HelpBoxMessageType.Warning);
            section.Add(_diagnosticsWarning);

            Action(section, "Open the diagnostics window", () => EditorApplication.ExecuteMenuItem(DiagnosticsMenu));
            return section;
        }

        #endregion

        #region ACTIONS

        private void GenerateAndSetUp()
        {
            CompositionResult result;
            try
            {
                result = PretzelSetup.GenerateComposition();
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is IOException)
            {
                _result.text = ex.Message;
                return;
            }

            if (result.AssetsPending)
            {
                _result.text = "Wrote " + string.Join(", ", result.WrittenFiles.Select(path => Path.GetFileName(path))) + ". The assets are created after compilation.";
                return;
            }

            ShowLog(result.AssetLog);
        }

        private void AddSceneScope()
        {
            Type scopeType = CompositionAssets.ScopeType;
            if (scopeType == null)
            {
                _result.text = "Install VContainer and the Pretzel VContainer module first.";
                return;
            }

            GameObject scope = new("LifetimeScope", scopeType);
            Undo.RegisterCreatedObjectUndo(scope, "Add LifetimeScope");
            Selection.activeGameObject = scope;
            _result.text = "Added a LifetimeScope to the open scene. Its parent is the root scope; add MonoInstallers to it for scene objects.";
        }

        private void InstallVContainer()
        {
            IReadOnlyList<CatalogEntry> catalog = CatalogService.GetEntries();
            CatalogEntry entry = catalog.FirstOrDefault(e => string.Equals(e.packageName, CompositionAssets.VContainerPackage, StringComparison.Ordinal));
            if (entry == null || !PackageService.Install(new[] { entry }, catalog, "Installing VContainer"))
            {
                _result.text = PackageService.LastError ?? "VContainer could not be installed. Use the Addons Extra tab.";
            }
        }

        private static void OpenGuide(string guidePath)
        {
            string path = Path.GetFullPath(guidePath);
            if (File.Exists(path))
            {
                EditorUtility.OpenWithDefaultApp(path);
            }
            else
            {
                Application.OpenURL(OfficialDocsUrl);
            }
        }

        private void ShowLog(IReadOnlyList<string> log)
        {
            if (_result != null)
            {
                _result.text = log.Count == 0 ? "Everything is already set up." : string.Join("\n", log);
                Refresh();
            }
        }

        #endregion

        #region STATE

        private void Rebuild()
        {
            Settings.Normalize();
            Refresh();
        }

        private void Refresh()
        {
            bool vcontainer = CompositionAssets.IsVContainerInstalled;
            bool integration = CompositionAssets.IsIntegrationActive;
            _vcontainerStatus.text = vcontainer ? "VContainer is installed." : "VContainer is not installed.";
            _integrationStatus.text = integration
                ? "Pretzel VContainer: PretzelLifetimeScope, ScriptableInstaller and MonoInstaller are available."
                : "Pretzel VContainer is not installed, or VContainer is missing. Install the Pretzel VContainer module from the Addons tab.";
            UnityEngine.Object vcontainerSettings = CompositionAssets.PreloadedSettings();
            UnityEngine.Object root = CompositionAssets.RootScopeOf(vcontainerSettings);
            _rootStatus.text = root != null
                ? $"Root scope: {root.name}, created before the first scene loads."
                : "No root scope yet. Use Generate and set up.";
            _install.style.display = vcontainer ? DisplayStyle.None : DisplayStyle.Flex;

            bool diagnostics = CompositionAssets.IsDiagnosticsEnabled(vcontainerSettings);
            _diagnostics.SetEnabled(vcontainerSettings != null);
            _diagnostics.label = vcontainerSettings != null ? "Enable diagnostics" : "Enable diagnostics (generate first)";
            _diagnostics.SetValueWithoutNotify(diagnostics);
            _diagnosticsWarning.style.display = diagnostics ? DisplayStyle.Flex : DisplayStyle.None;

            foreach (PretzelServiceOption option in PretzelServiceCatalog.All)
            {
                Toggle toggle = _pretzelToggles[option.Id];
                toggle.SetEnabled(option.IsInstalled);
                toggle.label = option.IsInstalled ? option.Label : option.Label + " (module not installed)";
                toggle.SetValueWithoutNotify(Settings.pretzelServices.Contains(option.Id, StringComparer.OrdinalIgnoreCase));
            }

            bool data = PretzelServiceCatalog.Find(PretzelServiceCatalog.Data).IsInstalled;
            _starter.SetEnabled(data || Settings.starterServices);
            _starter.SetValueWithoutNotify(Settings.starterServices);
            _starterExample.SetEnabled(Settings.starterServices);
            _starterExample.SetValueWithoutNotify(Settings.starterExample);
            _starterHint.text = data
                ? "Ready-to-use ISaveService (Pretzel Data: any type by DataKey, slots, async, corrupt files kept) and IGameService (menu, play, pause, " +
                  "game over, score and saved high score), registered in the root scope. The example loads progress and plays one short session on start."
                : "SaveService needs the Pretzel Data module. Install it from the Addons tab, or untick this.";

            ShowApiUrl();
            ShowAssemblyWarning();
            RebuildServices();
        }

        private void ShowApiUrl()
        {
            bool api = Settings.pretzelServices.Contains(PretzelServiceCatalog.Api, StringComparer.OrdinalIgnoreCase);
            _apiUrl.style.display = api ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ShowAssemblyWarning()
        {
            string asmdef = CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(Settings.codeFolder + "/Probe.cs");
            _assemblyWarning.style.display = asmdef == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (asmdef != null)
            {
                IEnumerable<string> references = new[] { "VContainer", "Pretzel.Integrations.VContainer" }
                    .Concat(CompositionGenerator.SelectedPretzelServices(Settings).Select(o => o.UsingNamespace));
                _assemblyWarning.text = $"The code folder belongs to {asmdef}. Add these references to it: {string.Join(", ", references)}.";
            }
        }

        private void RebuildServices()
        {
            _serviceRows.Clear();
            foreach (ServiceDefinition service in Settings.services)
            {
                _serviceRows.Add(CreateServiceRow(service));
            }
        }

        private VisualElement CreateServiceRow(ServiceDefinition service)
        {
            VisualElement row = new();
            row.AddToClassList("pretzel-section__row");

            TextField name = new() { value = service.name, isDelayed = true };
            name.style.flexGrow = 1f;
            name.RegisterValueChangedCallback(evt => Change(() => service.name = evt.newValue.Trim()));
            row.Add(name);

            EnumField lifetime = new(service.lifetime);
            lifetime.RegisterValueChangedCallback(evt => Change(() => service.lifetime = (ServiceLifetime)evt.newValue));
            row.Add(lifetime);

            row.Add(RowToggle("Interface", service.withInterface, v => service.withInterface = v));
            row.Add(RowToggle("IStartable", service.startable, v => service.startable = v));
            row.Add(RowToggle("ITickable", service.tickable, v => service.tickable = v));

            Button remove = new(() =>
            {
                Settings.services.Remove(service);
                _context.MarkProjectDirty();
                RebuildServices();
            })
            { text = "Remove" };
            row.Add(remove);
            return row;
        }

        private Toggle RowToggle(string text, bool value, Action<bool> set)
        {
            Toggle toggle = new() { text = text, value = value };
            toggle.RegisterValueChangedCallback(evt => Change(() => set(evt.newValue)));
            return toggle;
        }

        private void Change(Action apply)
        {
            apply();
            _context.MarkProjectDirty();
        }

        private string UniqueServiceName()
        {
            string name = "NewService";
            for (int i = 2; Settings.services.Exists(s => string.Equals(s.name, name, StringComparison.Ordinal)); i++)
            {
                name = "NewService" + i;
            }

            return name;
        }

        #endregion

        #region UI

        private static VisualElement Section(string title)
        {
            VisualElement section = new();
            section.AddToClassList("pretzel-section");
            Label label = new(title);
            label.AddToClassList("pretzel-section__title");
            section.Add(label);
            return section;
        }

        private static Label Hint(VisualElement parent)
        {
            Label hint = new();
            hint.AddToClassList("pretzel-section__hint");
            parent.Add(hint);
            return hint;
        }

        private static Button Action(VisualElement parent, string text, Action clicked, bool primary = false)
        {
            Button button = new(clicked) { text = text };
            button.AddToClassList("pretzel-section__action");
            if (primary)
            {
                button.AddToClassList("pretzel-section__action--primary");
            }

            parent.Add(button);
            return button;
        }

        private TextField Bind(VisualElement parent, string label, Func<string> get, Action<string> set)
        {
            TextField field = new(label) { value = get(), isDelayed = true };
            field.RegisterValueChangedCallback(evt =>
            {
                set(evt.newValue.Trim());
                _context.MarkProjectDirty();
                ShowAssemblyWarning();
            });
            _context.SettingsReset += () => field.SetValueWithoutNotify(get());
            parent.Add(field);
            return field;
        }

        #endregion
    }
}
