# Dev Console Usage

| File | What it shows |
|------|---------------|
| `GameCommands.cs` | Registering commands with arguments and text output on a `DeveloperConsole`. |

## Try it

1. Create a GameObject with a `UIDocument`. Assign a PanelSettings with a sort order above your game UI. The
   document needs no UXML; the console builds its UI in code.
2. Add `DeveloperConsole` and `GameCommands`.
3. Press Play and press the back-quote key (`` ` ``), or call `DeveloperConsole.Toggle()` from your own input code.
4. Type `help`, `timescale 0.5`, `god` or `fps`. Up and Down browse the history.

Rules:

- With **Development Builds Only** on (the default), the console destroys itself in release players.
- The toggle key uses the Input System when it is installed and active, otherwise the legacy Input Manager.
- Command handlers run on the main thread. Errors are shown as text instead of being thrown.
- Never ship commands that change saves or purchases without a release-build check of your own.
