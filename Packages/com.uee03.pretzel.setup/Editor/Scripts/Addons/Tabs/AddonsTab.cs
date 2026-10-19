using System;
using System.Collections.Generic;
using System.Linq;
using Pretzel.Setup.Settings;
using Pretzel.Setup.Addons.Catalog;
using Pretzel.Setup.Addons.Controls;
using Pretzel.Setup.Addons.Services;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UpmSource = UnityEditor.PackageManager.PackageSource;

namespace Pretzel.Setup.Addons.Tabs
{
    /// <summary>One card per Pretzel module, with install, remove and update.</summary>
    public sealed class AddonsTab : PackagesTabBase
    {
        public override string Title => "Addons";

        public override int Order => 10;

        protected override void BuildToolbar(VisualElement toolbar)
        {
            Button updateAll = new(UpdateAll) { text = "Update all", tooltip = "Pull the latest version of every installed Pretzel module that came from git." };
            toolbar.Add(updateAll);
        }

        private void UpdateAll()
        {
            PretzelProjectSettings settings = Context.Project;
            List<string> identifiers = new();
            foreach (ModuleInfo module in ModuleCatalog.All)
            {
                PackageInfo info = PackageService.Get(module.Name);
                string identifier = module.GetIdentifier(settings);
                if (info != null && info.source == UpmSource.Git && identifier != null)
                {
                    identifiers.Add(identifier);
                }
            }

            PackageService.InstallIdentifiers(identifiers, "Updating Pretzel modules");
        }

        protected override void BuildCards(VisualElement list)
        {
            PretzelProjectSettings settings = Context.Project;
            if (string.IsNullOrWhiteSpace(ModuleCatalog.ResolveSource(settings).Url))
            {
                list.Add(new HelpBox("Pretzel Setup was not installed from git, so set the Pretzel repository URL on the Settings tab to install modules. Modules that are already in the project are listed below.", HelpBoxMessageType.Info));
            }

            foreach (ModuleInfo module in ModuleCatalog.All)
            {
                list.Add(new PackageCard(CreateModel(module, settings)));
            }
        }

        private CardModel CreateModel(ModuleInfo module, PretzelProjectSettings settings)
        {
            PackageInfo info = PackageService.Get(module.Name);
            bool busy = PackageService.IsBusy;
            bool local = info != null && (info.source == UpmSource.Embedded || info.source == UpmSource.Local);

            CardModel model = new()
            {
                Title = module.DisplayName,
                Subtitle = module.Name,
                Description = module.Description,
                Detail = module.Dependencies.Count == 0 ? null : "Depends on: " + string.Join(", ", module.Dependencies.Select(DisplayName))
            };

            if (info == null)
            {
                model.Chips.Add(new CardChip("Not installed", ChipKind.Missing));
            }
            else
            {
                model.Chips.Add(new CardChip(local ? "Local " + info.version : info.version, ChipKind.Ok));
            }

            if (module.OptionalLibraries.Count > 0)
            {
                List<string> missing = new();
                foreach ((string libraryName, string packageName) in module.OptionalLibraries)
                {
                    bool present = PackageService.IsInstalled(packageName);
                    model.Chips.Add(new CardChip(present ? libraryName : libraryName + " not installed", present ? ChipKind.Ok : ChipKind.Neutral));
                    if (!present)
                    {
                        missing.Add(libraryName);
                    }
                }

                if (missing.Count > 0)
                {
                    model.Note = $"Each adapter compiles only when its library is installed. Not installed: {string.Join(", ", missing)}. Install them on the Addons Extra tab.";
                    model.NoteType = HelpBoxMessageType.Info;
                }
            }

            string identifier = module.GetIdentifier(settings);
            if (info == null)
            {
                model.Actions.Add(new CardAction("Install", () => Install(module, settings), !busy && identifier != null, primary: true,
                    tooltip: identifier == null ? "Set the repository URL on the Settings tab." : null));
                return model;
            }

            if (!local && info.source == UpmSource.Git && identifier != null)
            {
                bool outdated = IsOutdated(info, settings);
                if (outdated)
                {
                    model.Chips.Add(new CardChip("Update to " + ModuleCatalog.ResolveSource(settings).Version, ChipKind.Warn));
                }

                // Re-adding the git identifier makes Package Manager fetch the newest commit of the branch or tag.
                model.Actions.Add(new CardAction("Update", () => PackageService.InstallIdentifiers(new[] { identifier }, "Updating " + module.DisplayName), !busy, primary: outdated,
                    tooltip: "Pull the latest version from the repository."));
            }

            if (info.source == UpmSource.Embedded)
            {
                // Removing an embedded package would delete its folder from Packages.
                model.Actions.Add(new CardAction("Uninstall", null, false, danger: true,
                    tooltip: "Embedded in this project. Delete its folder under Packages to remove it."));
                return model;
            }

            string dependent = InstalledDependent(module);
            model.Actions.Add(new CardAction("Uninstall", () => PackageService.Uninstall(module.Name, module.DisplayName), !busy && dependent == null, danger: true,
                tooltip: dependent == null ? null : "Required by " + dependent + "."));
            return model;
        }

        private static void Install(ModuleInfo module, PretzelProjectSettings settings)
        {
            List<string> identifiers = new();
            Collect(module, settings, identifiers);
            PackageService.InstallIdentifiers(identifiers, "Installing " + module.DisplayName);
        }

        // Dependencies are listed explicitly, so a git source for one module does not rely on the others being resolvable.
        private static void Collect(ModuleInfo module, PretzelProjectSettings settings, List<string> identifiers)
        {
            string identifier = module.GetIdentifier(settings);
            if (identifier == null || PackageService.IsInstalled(module.Name) || identifiers.Contains(identifier))
            {
                return;
            }

            identifiers.Add(identifier);
            foreach (string dependency in module.Dependencies)
            {
                ModuleInfo other = ModuleCatalog.All.FirstOrDefault(m => m.Name == dependency);
                if (other != null)
                {
                    Collect(other, settings, identifiers);
                }
            }
        }

        private static string InstalledDependent(ModuleInfo module)
        {
            ModuleInfo dependent = ModuleCatalog.All.FirstOrDefault(m => m.Dependencies.Contains(module.Name) && PackageService.IsInstalled(m.Name));
            return dependent?.DisplayName;
        }

        private static bool IsOutdated(PackageInfo info, PretzelProjectSettings settings)
        {
            // A branch name has no version to compare, so only tags that look like versions are checked.
            string tag = (ModuleCatalog.ResolveSource(settings).Version ?? string.Empty).TrimStart('v');
            return info.source == UpmSource.Git && Version.TryParse(tag, out _) && info.version != tag;
        }

        private static string DisplayName(string packageName)
        {
            return ModuleCatalog.All.FirstOrDefault(m => m.Name == packageName)?.DisplayName ?? packageName;
        }
    }
}
