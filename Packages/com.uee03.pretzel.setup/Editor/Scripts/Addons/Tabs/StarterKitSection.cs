using System.Collections.Generic;
using System.Linq;
using Pretzel.Setup.Folders;
using Pretzel.Setup.Settings;
using Pretzel.Setup.Tabs;
using Pretzel.Setup.Addons.Catalog;
using Pretzel.Setup.Addons.Services;
using UnityEngine.UIElements;

namespace Pretzel.Setup.Addons.Tabs
{
    /// <summary>
    /// The first-run card on the Main tab: pick Game, Tool or System, then create the folders and install the recommended addons.
    /// Stays until it is used or skipped.
    /// </summary>
    public sealed class StarterKitSection : IPretzelMainSection
    {
        // Catalog ids that need no manual NuGet step, so one click can install them.
        private static readonly Dictionary<string, string[]> Recommended = new()
        {
            [FolderPreset.Game] = new[] { "litmotion", "unitask", "vcontainer" },
            [FolderPreset.Tool] = new[] { "unitask" },
            [FolderPreset.Systems] = new[] { "unitask" }
        };

        public int Order => -100;

        public VisualElement CreateSection(PretzelTabContext context)
        {
            if (context.User.starterKitDone)
            {
                return null;
            }

            VisualElement section = new();
            section.AddToClassList("pretzel-section");
            section.AddToClassList("pretzel-starter");
            section.styleSheets.Add(SetupAssets.Style("Addons"));

            Label title = new("Welcome to Pretzel");
            title.AddToClassList("pretzel-section__title");
            section.Add(title);

            Label hint = new("Pick a starting point. Pretzel creates the folder structure and installs the addons you tick.");
            hint.AddToClassList("pretzel-section__hint");
            section.Add(hint);

            DropdownField kit = new("Project type", new List<string> { FolderPreset.Game, FolderPreset.Tool, FolderPreset.Systems }, 0);
            section.Add(kit);

            VisualElement addons = new();
            section.Add(addons);

            Dictionary<string, Toggle> toggles = new();
            void ShowAddons()
            {
                addons.Clear();
                toggles.Clear();
                List<CatalogEntry> catalog = CatalogService.GetEntries();
                foreach (string id in Recommended[kit.value])
                {
                    CatalogEntry entry = catalog.FirstOrDefault(e => e.id == id);
                    if (entry == null || PackageService.IsInstalled(entry.packageName))
                    {
                        continue;
                    }

                    Toggle toggle = new(entry.displayName + " " + entry.version) { value = true, tooltip = entry.description };
                    toggles[id] = toggle;
                    addons.Add(toggle);
                }
            }

            kit.RegisterValueChangedCallback(_ => ShowAddons());
            ShowAddons();

            VisualElement row = new();
            row.AddToClassList("pretzel-section__row");

            Button apply = new(() =>
            {
                Apply(context, kit.value, toggles);
                section.RemoveFromHierarchy();
            })
            { text = "Set up project" };
            apply.AddToClassList("pretzel-section__action");
            apply.AddToClassList("pretzel-section__action--primary");
            row.Add(apply);

            Button skip = new(() =>
            {
                Finish(context);
                section.RemoveFromHierarchy();
            })
            { text = "Skip" };
            skip.AddToClassList("pretzel-section__action");
            row.Add(skip);

            section.Add(row);
            return section;
        }

        private static void Apply(PretzelTabContext context, string kit, Dictionary<string, Toggle> toggles)
        {
            context.Project.activeFolderPreset = kit;
            context.MarkProjectDirty();
            ProjectFolders.Create(context.Project.ActivePreset.folders);
            context.RaiseSettingsReset();

            List<CatalogEntry> catalog = CatalogService.GetEntries();
            List<CatalogEntry> chosen = toggles
                .Where(pair => pair.Value.value)
                .Select(pair => catalog.FirstOrDefault(e => e.id == pair.Key))
                .Where(entry => entry != null)
                .ToList();

            if (chosen.Count > 0)
            {
                PackageService.Install(chosen, catalog, "Installing " + string.Join(", ", chosen.Select(e => e.displayName)));
            }

            Finish(context);
        }

        private static void Finish(PretzelTabContext context)
        {
            context.User.starterKitDone = true;
            context.MarkUserDirty();
        }
    }
}
