namespace Pretzel.Samples.Data
{
    /// <summary>A reward stored behind an interface. Newtonsoft writes the concrete type, limited by <see cref="KnownTypesBinder"/>.</summary>
    public interface IQuestReward
    {
        string Describe();
    }
}
