namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Takes a plain value in its constructor, supplied with WithParameter.</summary>
    public sealed class Greeter
    {
        private readonly string _greeting;

        public Greeter(string greeting) => _greeting = greeting;

        public string Greet(string name) => $"{_greeting}, {name}!";
    }
}
