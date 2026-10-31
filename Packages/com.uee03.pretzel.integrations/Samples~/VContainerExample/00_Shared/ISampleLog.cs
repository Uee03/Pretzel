namespace Pretzel.Samples.VContainerExample
{
    /// <summary>What every demo depends on to report what happened.</summary>
    public interface ISampleLog
    {
        void Write(string topic, string message);
    }
}
