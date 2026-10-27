using Pretzel.Setup.Addons.Catalog;
using Pretzel.Setup.Tabs;
using UnityEditor;
using UnityEngine.UIElements;

namespace Pretzel.Samples.Setup
{
    /// <summary>
    /// A card at the top of the Main tab. It only appears while no module source is known, which shows
    /// how a section hides itself by returning null.
    /// </summary>
    public sealed class ModuleSourceSection : IPretzelMainSection
    {
        public int Order => 100;

        public VisualElement CreateSection(PretzelTabContext context)
        {
            // The Settings value wins, else the git URL Pretzel Setup itself was installed from.
            if (!string.IsNullOrWhiteSpace(ModuleCatalog.ResolveSource(context.Project).Url))
            {
                return null;
            }

            VisualElement section = new();
            section.AddToClassList("pretzel-section");

            Label title = new("Module source not set");
            title.AddToClassList("pretzel-section__title");
            section.Add(title);

            Label hint = new("The Addons tab installs Pretzel modules from a git repository. Set the repository URL on the Settings tab, or keep using local packages.");
            hint.AddToClassList("pretzel-section__hint");
            section.Add(hint);

            Button learnMore = new(() => EditorUtility.DisplayDialog("Module source",
                "Settings tab > Repository URL, for example https://github.com/owner/Pretzel.git. An optional tag or branch pins the version.", "OK"))
            {
                text = "How do I set it?"
            };
            learnMore.AddToClassList("pretzel-section__action");
            section.Add(learnMore);
            return section;
        }
    }
}
