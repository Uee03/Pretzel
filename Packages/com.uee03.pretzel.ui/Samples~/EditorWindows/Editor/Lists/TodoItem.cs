namespace Pretzel.Samples.UI.EditorWindows
{
    /// <summary>The data model behind the Lists window. A plain class, so the list items are references that can be edited in place.</summary>
    public sealed class TodoItem
    {
        public string Title { get; set; }

        public bool Done { get; set; }
    }
}
