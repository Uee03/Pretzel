using System.Collections.Generic;
using System.Linq;
using Pretzel.Setup.Addons.Catalog;
using Pretzel.Setup.Addons.Controls;
using Pretzel.Setup.Addons.Services;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Pretzel.Setup.Addons.Tabs
{
    /// <summary>The third-party catalog: search, pinned versions, installed state and registry notes.</summary>
    public sealed class AddonsExtraTab : PackagesTabBase
    {
        private ToolbarSearchField _search;
        private bool _refreshing;

        public override string Title => "Addons Extra";

        public override int Order => 20;

        protected override void BuildToolbar(VisualElement toolbar)
        {
            _search = new ToolbarSearchField();
            _search.AddToClassList("pretzel-packages__search");
            _search.RegisterValueChangedCallback(_ => Rebuild());
            toolbar.Add(_search);

            Button refresh = new(RefreshSources) { text = "Reload catalogs", tooltip = "Reload the catalog sources listed on the Settings tab." };
            toolbar.Add(refresh);
        }

        private async void RefreshSources()
        {
            if (_refreshing)
            {
                return;
            }

            _refreshing = true;
            try
            {
                await CatalogService.RefreshAsync();
            }
            finally
            {
                _refreshing = false;
            }
        }

        protected override void BuildCards(VisualElement list)
        {
            foreach (string error in CatalogService.LoadErrors)
            {
                list.Add(new HelpBox("Could not load catalog " + error, HelpBoxMessageType.Warning));
            }

            List<CatalogEntry> catalog = CatalogService.GetEntries();
            string filter = _search?.value?.Trim() ?? string.Empty;

            foreach (CatalogEntry entry in catalog)
            {
                if (Matches(entry, filter))
                {
                    list.Add(new PackageCard(CreateModel(entry, catalog)));
                }
            }
        }

        private static bool Matches(CatalogEntry entry, string filter)
        {
            if (filter.Length == 0)
            {
                return true;
            }

            return Contains(entry.displayName, filter) || Contains(entry.description, filter) || Contains(entry.packageName, filter);
        }

        private static bool Contains(string text, string filter)
        {
            return text != null && text.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private CardModel CreateModel(CatalogEntry entry, List<CatalogEntry> catalog)
        {
            PackageInfo info = PackageService.Get(entry.packageName);
            bool busy = PackageService.IsBusy;

            CardModel model = new()
            {
                Title = string.IsNullOrEmpty(entry.displayName) ? entry.packageName : entry.displayName,
                Subtitle = entry.packageName + "  (" + SourceName(entry.source) + ")",
                Description = entry.description,
                Note = entry.registryNote
            };

            model.Chips.Add(info == null ? new CardChip("Not installed", ChipKind.Missing) : new CardChip(info.version, ChipKind.Ok));
            model.Chips.Add(new CardChip("Pinned " + entry.version, ChipKind.Neutral));

            bool needsNuGetPackage = entry.NeedsNuGet && !NuGetState.IsInstalled(entry.nugetPackage);
            if (entry.NeedsNuGet)
            {
                model.Chips.Add(new CardChip(needsNuGetPackage ? "NuGet " + entry.nugetPackage + " needed" : "NuGet " + entry.nugetPackage, needsNuGetPackage ? ChipKind.Warn : ChipKind.Ok));
            }

            List<string> together = entry.dependencies.Select(id => catalog.FirstOrDefault(e => e.id == id)?.displayName ?? id).ToList();
            if (together.Count > 0)
            {
                model.Detail = "Installs with: " + string.Join(", ", together);
            }

            model.Extras.Add(CreatePinField(entry));

            bool nuGetForUnityInstalled = catalog.Any(e => e.id == BuiltInCatalog.NuGetForUnityId && PackageService.IsInstalled(e.packageName));

            if (info == null)
            {
                if (entry.NeedsNuGet && !nuGetForUnityInstalled)
                {
                    model.Actions.Add(new CardAction("Install NuGetForUnity", () => InstallNuGetForUnity(catalog), !busy, primary: true,
                        tooltip: "NuGetForUnity installs the " + entry.nugetPackage + " NuGet package."));
                }
                else if (needsNuGetPackage)
                {
                    model.Actions.Add(new CardAction("Open NuGet window", () => OpenNuGetWindow(), !busy, primary: true,
                        tooltip: "Search " + entry.nugetPackage + " and press Install, then come back here."));
                    model.Actions.Add(new CardAction("Install", null, false, tooltip: "Install the " + entry.nugetPackage + " NuGet package first."));
                }
                else
                {
                    model.Actions.Add(new CardAction("Install", () => PackageService.Install(new[] { entry }, catalog, "Installing " + model.Title), !busy, primary: true));
                }
            }
            else
            {
                string dependent = InstalledDependent(entry, catalog);
                model.Actions.Add(new CardAction("Uninstall", () => PackageService.Uninstall(entry.packageName, model.Title), !busy && dependent == null, danger: true,
                    tooltip: dependent == null ? null : "Required by " + dependent + "."));
            }

            if (!string.IsNullOrEmpty(entry.homepage))
            {
                string url = entry.homepage;
                model.Actions.Add(new CardAction("Open page", () => Application.OpenURL(url)));
            }

            return model;
        }

        private VisualElement CreatePinField(CatalogEntry entry)
        {
            TextField field = new("Pinned version") { isDelayed = true };
            field.SetValueWithoutNotify(entry.version);
            field.tooltip = "Saved with the project. A git source uses it as the tag.";
            field.RegisterValueChangedCallback(evt =>
            {
                CatalogService.SetPin(entry.id, evt.newValue);
                Context.MarkProjectDirty();
                Rebuild();
            });
            return field;
        }

        private static void InstallNuGetForUnity(List<CatalogEntry> catalog)
        {
            CatalogEntry nuGet = catalog.FirstOrDefault(e => e.id == BuiltInCatalog.NuGetForUnityId);
            if (nuGet != null)
            {
                PackageService.Install(new[] { nuGet }, catalog, "Installing NuGetForUnity");
            }
        }

        private static void OpenNuGetWindow()
        {
            if (!NuGetState.OpenWindow())
            {
                Debug.LogWarning("[Pretzel] Could not open the NuGet window. Open it from the NuGet menu once NuGetForUnity has finished installing.");
            }
        }

        private static string InstalledDependent(CatalogEntry entry, List<CatalogEntry> catalog)
        {
            CatalogEntry dependent = catalog.FirstOrDefault(e => e.dependencies != null && e.dependencies.Contains(entry.id) && PackageService.IsInstalled(e.packageName));
            if (dependent != null)
            {
                return dependent.displayName;
            }

            ModuleInfo module = ModuleCatalog.All.FirstOrDefault(m => m.Packages.Contains(entry.packageName) && PackageService.IsInstalled(m.Name));
            return module == null ? null : "Pretzel " + module.DisplayName;
        }

        private static string SourceName(CatalogSource source)
        {
            return source switch
            {
                CatalogSource.GitUrl => "git",
                CatalogSource.OpenUpm => "OpenUPM",
                _ => "Unity registry"
            };
        }
    }
}
