namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Gives every instance a number, so the log shows when the container creates a new one.</summary>
    public abstract class NumberedObject
    {
        private static int _created;

        protected NumberedObject() => Id = ++_created;

        public int Id { get; }
    }
}
