namespace Pretzel.Inputs
{
    /// <summary>Where binding overrides are kept between sessions.</summary>
    public interface IBindingStore
    {
        /// <summary>The saved overrides as JSON, or an empty string when there are none.</summary>
        string Load();

        void Save(string json);
    }
}
