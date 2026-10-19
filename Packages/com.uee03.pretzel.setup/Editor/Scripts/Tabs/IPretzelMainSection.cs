using UnityEngine.UIElements;

namespace Pretzel.Setup.Tabs
{
    /// <summary>
    /// A card shown at the top of the Main tab, contributed by another package. Found with TypeCache,
    /// so the Main tab needs no reference to the package that provides it.
    /// </summary>
    public interface IPretzelMainSection
    {
        /// <summary>Lower values come first.</summary>
        int Order { get; }

        /// <summary>Returns the section, or null when there is nothing to show.</summary>
        VisualElement CreateSection(PretzelTabContext context);
    }
}
