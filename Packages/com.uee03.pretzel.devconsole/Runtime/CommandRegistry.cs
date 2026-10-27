using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pretzel.Core;

namespace Pretzel.DevConsole
{
    /// <summary>
    /// Named commands with string arguments. Names are case-insensitive. Arguments are split on spaces, and double quotes
    /// group words. Includes a <c>help</c> command.
    /// </summary>
    public sealed class CommandRegistry
    {
        private readonly Dictionary<string, ConsoleCommand> _commands = new(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<ConsoleCommand> Commands => _commands.Values.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase);

        public CommandRegistry()
        {
            Register("help", "Lists every command.", _ => Help());
        }

        /// <param name="handler">Receives the arguments after the name and returns text to show, or null.</param>
        public void Register(string name, string description, Func<string[], string> handler)
        {
            Guard.NotNullOrEmpty(name, nameof(name));
            Guard.NotNull(handler, nameof(handler));
            if (name.IndexOf(' ') >= 0)
            {
                throw new ArgumentException("A command name cannot contain spaces.", nameof(name));
            }

            _commands[name] = new ConsoleCommand(name, description, handler);
        }

        public void Register(string name, string description, Action<string[]> handler)
        {
            Guard.NotNull(handler, nameof(handler));
            Register(name, description, args =>
            {
                handler(args);
                return null;
            });
        }

        public bool Unregister(string name) => _commands.Remove(name);

        public bool Contains(string name) => _commands.ContainsKey(name);

        /// <summary>Runs a command line. Errors are returned as text, never thrown.</summary>
        public string Execute(string line)
        {
            string[] tokens = Tokenize(line);
            if (tokens.Length == 0)
            {
                return null;
            }

            if (!_commands.TryGetValue(tokens[0], out ConsoleCommand command))
            {
                return $"Unknown command '{tokens[0]}'. Type help for a list.";
            }

            string[] arguments = new string[tokens.Length - 1];
            Array.Copy(tokens, 1, arguments, 0, arguments.Length);
            try
            {
                return command.Handler(arguments);
            }
            catch (Exception ex)
            {
                return $"{command.Name} failed: {ex.Message}";
            }
        }

        public static string[] Tokenize(string line)
        {
            List<string> tokens = new();
            if (string.IsNullOrWhiteSpace(line))
            {
                return tokens.ToArray();
            }

            StringBuilder current = new();
            bool quoted = false;
            bool hasToken = false;
            foreach (char c in line)
            {
                if (c == '"')
                {
                    quoted = !quoted;
                    hasToken = true;
                }
                else if (char.IsWhiteSpace(c) && !quoted)
                {
                    if (hasToken)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                        hasToken = false;
                    }
                }
                else
                {
                    current.Append(c);
                    hasToken = true;
                }
            }

            if (hasToken)
            {
                tokens.Add(current.ToString());
            }

            return tokens.ToArray();
        }

        private string Help()
        {
            StringBuilder text = new();
            foreach (ConsoleCommand command in Commands)
            {
                text.Append(command.Name);
                if (command.Description.Length > 0)
                {
                    text.Append(" - ").Append(command.Description);
                }

                text.AppendLine();
            }

            return text.ToString().TrimEnd();
        }
    }
}
