using UnityEngine.UIElements;

namespace Pretzel.Setup.Tabs
{
    /// <summary>
    /// A tab of the Project Setup window. Implementations are found with TypeCache, so a package contributes a tab
    /// by adding a class with a parameterless constructor. They appear sorted by <see cref="Order"/>.
    /// </summary>
    public interface IPretzelTab
    {
        string Title { get; }

        int Order { get; }

        /// <summary>Builds the tab content. Called once per window.</summary>
        VisualElement CreateView(PretzelTabContext context);

        /// <summary>Called each time the tab is shown, to refresh state that can change outside the window.</summary>
        void OnSelected()
        {
        }
    }
}
