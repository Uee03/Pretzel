using System;

namespace Pretzel.DevConsole
{
    public sealed class ConsoleCommand
    {
        public string Name { get; }
        public string Description { get; }
        internal Func<string[], string> Handler { get; }

        internal ConsoleCommand(string name, string description, Func<string[], string> handler)
        {
            Name = name;
            Description = description ?? string.Empty;
            Handler = handler;
        }
    }
}
