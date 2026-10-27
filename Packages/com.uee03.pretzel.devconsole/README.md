# Pretzel Dev Console

An in-game log overlay and command line for development builds, built with UI Toolkit. Depends on Pretzel Core.

## Setup

Add a `UIDocument` (with a PanelSettings sorted above your game UI; no UXML needed) and `DeveloperConsole` to a
GameObject. Press the back-quote key or call `Toggle()`.

- **Development Builds Only** (on by default) destroys the console in release players. The Editor counts as a
  development build.
- The toggle key uses the Input System when it is installed and active, otherwise the legacy Input Manager.
- Logs from any thread are captured into a ring buffer of **Capacity** entries. Filters show logs, warnings and errors;
  the toolbar shows counts. The list is virtualized, so a full buffer stays cheap.
- Enter runs a command, Up and Down browse the history.

## Commands

```csharp
DeveloperConsole console = GetComponent<DeveloperConsole>();
console.Commands.Register("give", "give <item> [count]", args => inventory.Add(args[0], args.Length > 1 ? int.Parse(args[1]) : 1));
console.Commands.Register("fps", "Shows the frame rate.", _ => $"{1f / Time.unscaledDeltaTime:0} fps");
console.Submit("give \"iron sword\" 2");
```

Names are case-insensitive; double quotes group words. A handler that returns text shows it in the console. Exceptions
in handlers are shown as text, not thrown. `help` and `clear` are built in.

`CommandRegistry` and `LogBuffer` are plain classes, usable without the component (for example for a remote console).

## Sample

`Samples~/DevConsoleUsage`: timescale, god mode and fps commands.
