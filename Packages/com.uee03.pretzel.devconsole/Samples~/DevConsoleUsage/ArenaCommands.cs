using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Pretzel.DevConsole;
using UnityEngine;

namespace Pretzel.Samples.Console
{
    /// <summary>
    /// The console commands of the arena. Every command talks to a real game object (health, wallet, enemies, tuning), so
    /// typing it changes what you see. The class is plain C#: it takes its dependencies in the constructor and is easy to test.
    /// </summary>
    public sealed class ArenaCommands
    {
        private readonly ArenaGame _game;

        public ArenaCommands(ArenaGame game)
        {
            _game = game;
        }

        public void Register(CommandRegistry commands)
        {
            commands.Register("god", "god [on|off] - the player takes no damage.", God);
            commands.Register("hp", "Shows the player's hit points.", _ => Health());
            commands.Register("heal", "heal [amount] - restores hit points, all of them without an amount.", Heal);
            commands.Register("damage", "damage <amount> - hurts the player.", Damage);
            commands.Register("revive", "Brings a dead player back with full health.", _ => Revive());
            commands.Register("spawn", "spawn [count] - adds enemies, up to maxEnemies.", Spawn);
            commands.Register("killall", "Removes every enemy.", _ => $"Removed {_game.RemoveAllEnemies()} enemies.");
            commands.Register("gold", "gold <amount> - adds (or removes) gold.", Gold);
            commands.Register("tp", "tp <x> <z> - moves the player.", Teleport);
            commands.Register("tuning", "Lists every tuning value and its current number.", _ => ListTuning());
            commands.Register("get", "get <name> - reads a tuning value.", Get);
            commands.Register("set", "set <name> <value> - changes a tuning value, for example set enemySpeed 5.", Set);
            commands.Register("reset", "Restarts the arena: no enemies, full health, no gold.", _ =>
            {
                _game.ResetArena();
                return "Arena reset.";
            });
            commands.Register("timescale", "timescale <value> - sets Time.timeScale.", TimeScale);
            commands.Register("fps", "Shows the current frame rate.", _ => $"{1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime):0} fps");
        }

        private string God(string[] args)
        {
            if (!CommandArguments.TryToggle(args, _game.Player.IsGodMode, out bool enabled))
            {
                return "Usage: god [on|off]";
            }

            _game.Player.IsGodMode = enabled;
            return enabled ? "God mode on." : "God mode off.";
        }

        private string Health()
        {
            PlayerHealth player = _game.Player;
            return $"HP {player.Current:0}/{player.Max:0}{(player.IsGodMode ? " (god mode)" : string.Empty)}";
        }

        private string Heal(string[] args)
        {
            if (args.Length == 0)
            {
                _game.Player.Heal(_game.Player.Max);
            }
            else if (CommandArguments.TryFloat(args, 0, out float amount) && amount > 0f)
            {
                _game.Player.Heal(amount);
            }
            else
            {
                return "Usage: heal [amount]";
            }

            return Health();
        }

        private string Damage(string[] args)
        {
            if (!CommandArguments.TryFloat(args, 0, out float amount) || amount <= 0f)
            {
                return "Usage: damage <amount>";
            }

            _game.Player.Damage(amount);
            return _game.Player.IsGodMode ? "God mode is on: no damage taken." : Health();
        }

        private string Revive()
        {
            if (!_game.Player.IsDead)
            {
                return "The player is alive.";
            }

            _game.Player.Revive();
            return Health();
        }

        private string Spawn(string[] args)
        {
            int requested = 1;
            if (args.Length > 0 && (!CommandArguments.TryInt(args, 0, out requested) || requested < 1))
            {
                return "Usage: spawn [count]";
            }

            int room = Mathf.Max(0, Mathf.FloorToInt(_game.Tuning.MaxEnemies) - _game.Enemies.Count);
            int count = Mathf.Min(requested, room);
            for (int i = 0; i < count; i++)
            {
                _game.SpawnEnemy();
            }

            return count < requested
                ? $"Spawned {count} of {requested}: maxEnemies is {_game.Tuning.MaxEnemies:0}. Enemies: {_game.Enemies.Count}."
                : $"Spawned {count}. Enemies: {_game.Enemies.Count}.";
        }

        private string Gold(string[] args)
        {
            if (!CommandArguments.TryInt(args, 0, out int amount))
            {
                return "Usage: gold <amount>";
            }

            _game.AddGold(amount);
            return $"Gold: {_game.Gold}";
        }

        private string Teleport(string[] args)
        {
            if (!CommandArguments.TryFloat(args, 0, out float x) || !CommandArguments.TryFloat(args, 1, out float z))
            {
                return "Usage: tp <x> <z>";
            }

            _game.Teleport(x, z);
            return $"Moved to {x:0.#}, {z:0.#}.";
        }

        private string ListTuning()
        {
            List<string> lines = _game.Tuning.Names
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Select(name => _game.Tuning.TryGet(name, out float value) ? $"{name} = {value.ToString("0.##", CultureInfo.InvariantCulture)}" : name)
                .ToList();
            return string.Join("\n", lines);
        }

        private string Get(string[] args)
        {
            if (args.Length != 1)
            {
                return "Usage: get <name>. Type tuning for the names.";
            }

            return _game.Tuning.TryGet(args[0], out float value)
                ? $"{args[0]} = {value.ToString("0.##", CultureInfo.InvariantCulture)}"
                : $"Unknown value '{args[0]}'. Type tuning for the names.";
        }

        private string Set(string[] args)
        {
            if (args.Length != 2 || !CommandArguments.TryFloat(args, 1, out float value))
            {
                return "Usage: set <name> <value>";
            }

            return _game.Tuning.TrySet(args[0], value)
                ? $"{args[0]} = {value.ToString("0.##", CultureInfo.InvariantCulture)}"
                : $"Unknown value '{args[0]}'. Type tuning for the names.";
        }

        private static string TimeScale(string[] args)
        {
            if (!CommandArguments.TryFloat(args, 0, out float scale) || scale < 0f)
            {
                return "Usage: timescale <value>";
            }

            Time.timeScale = scale;
            return $"Time scale is {scale}.";
        }
    }
}
