# Dev Console Usage

A small playable arena where every console command changes something real: health, enemies, gold, speeds and time.

| File | What it shows |
|------|---------------|
| `ArenaGame.cs` | Builds the arena at runtime from primitives (any render pipeline): player, chasing enemies, gold, camera and light. |
| `PlayerHealth.cs` | Hit points with a `IsGodMode` flag that `Damage` honors. The `god` command sets it. |
| `PlayerController.cs`, `Enemy.cs` | Movement and combat that read `ArenaTuning` every frame, so `set enemySpeed 6` shows immediately. |
| `ArenaTuning.cs` | Named tuning values with getters and setters: one generic `get`/`set`/`tuning` command set instead of a command per number. |
| `ArenaCommands.cs` | The commands, in a plain class that takes the game in its constructor. Argument checks, usage text and results as text. |
| `CommandArguments.cs` | Culture-invariant parsing helpers (`TryFloat`, `TryInt`, `TryToggle`). |
| `ArenaHud.cs` | A text HUD, so you see the effect of a command. |
| `GameCommands.cs` | The glue: registers `ArenaCommands` on the console and stops typing from moving the player. |

## Try it

1. Create a GameObject named `Arena` and add `ArenaGame` and `ArenaHud`.
2. Create another GameObject with a `UIDocument` (assign a PanelSettings with a sort order above your UI; no UXML needed)
   and add `DeveloperConsole` and `GameCommands`.
3. Press Play. Move with WASD or the arrow keys; enemies chase you and your aura defeats the nearest one for gold.
4. Open the console with the back-quote key and try:

| Command | Effect on the game |
|---------|--------------------|
| `god`, `god off` | The player takes no damage (HUD shows GOD MODE). |
| `damage 30`, `heal`, `hp`, `revive` | Changes the player's health. |
| `spawn 10`, `killall` | Adds enemies (capped by `maxEnemies`) or removes them. |
| `gold 500` | Adds gold to the HUD. |
| `tp 5 -3` | Moves the player. |
| `tuning`, `get enemySpeed`, `set enemySpeed 6`, `set spawnInterval 0` | Reads and changes the tuning values live. `spawnInterval 0` stops automatic spawns. |
| `timescale 0.25`, `fps`, `reset`, `help` | Slow motion, frame rate, restart, command list. |

## Patterns worth copying

- Keep commands in a plain class that receives the objects it changes. `GameCommands` is the only MonoBehaviour.
- Commands change the same state the game reads, never a copy. Here that is `PlayerHealth` and `ArenaTuning`.
- Validate arguments and return a `Usage:` line instead of throwing; the console also catches exceptions as text.
- Expose numbers as named tuning values once, and the generic `get`/`set` commands cover them all.
- Block gameplay input while the console is open (`ArenaGame.InputBlocked`).

Rules:

- With **Development Builds Only** on (the default), the console destroys itself in release players, and so do the commands.
- Command handlers run on the main thread. Errors are shown as text instead of being thrown.
- Never ship commands that change saves or purchases without a release-build check of your own.
