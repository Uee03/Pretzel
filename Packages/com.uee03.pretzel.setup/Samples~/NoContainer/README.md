# No Container

A small, complete game built **without a dependency injection container**. The shared services live in one ScriptableObject that a bootstrap method fills before the first scene loads, as described in [Without a container](../../Documentation~/WithoutAContainer.md).

*Click Rush*: press Play, click as often as you can before the time runs out, and beat your best score. It is small on purpose, so the structure is easy to see and copy.

## Needs

Pretzel **Data**, **Events**, **Preferences** and **Scenes**, and UI Toolkit (built in). The assemblies compile only when all four modules are installed. Install them from **Pretzel > Project Setup > Addons**.

## Set up

1. Import the sample from Package Manager > Pretzel Setup > Samples.
2. Run **Pretzel > Samples > No Container > Add Scenes To Build Settings** (the scenes load by name, so they must be in the build).
3. Open `Scenes/NoContainer_Menu` and press Play. You can also start from `NoContainer_Game`: the bootstrap runs before any scene.

To rebuild the scenes and assets after you change the scripts, run **Build Scenes And Assets** from the same menu. **Delete Saved Progress** clears the save file.

## Where things are

| Piece | File | Role |
|---|---|---|
| Services asset | `Runtime/GameServices.cs`, `Resources/GameServices.asset` | Creates and holds the event bus, scene loader, preferences and progress. `Initialize` builds them in order; `Shutdown` saves and drops them. |
| Bootstrap | `Runtime/GameServicesBootstrap.cs` | `BeforeSceneLoad` entry point. Loads the asset from `Resources` and initializes it. |
| Rules | `Runtime/Game/GameRound.cs` | The round's clock and score as plain C#. No Unity types. |
| Controller | `Runtime/Game/GameController.cs` | Runs a round and publishes `ScoreChanged`, `TimeChanged` and `RoundFinished`. No UI. |
| Screens | `Runtime/UI/MenuScreen.cs`, `GameScreen.cs` | UI Toolkit views. They listen to the event bus and tell the controller what the player did. |
| Save | `Runtime/Progress/*` | `IProgressService` over Pretzel Data. An unreadable save is kept, not overwritten. |
| Settings | `MenuScreen`, `GameServices.ApplyAudio` | Volume and mute are stored in Pretzel Preferences and applied to the `AudioListener`. |
| Styles | `Styles/*` | A USS file and the theme that imports it. |
| Tools | `Editor/NoContainerSampleBuilder.cs` | Builds the scenes and assets, registers the scenes for builds. |
| Tests | `Tests/Editor/*` | Round rules, saving, and the services' lifecycle. |

## What makes it production-ready

- **One place creates services**, in dependency order, so startup is predictable and any scene is a valid start scene.
- **Runtime state is not serialized.** The asset holds only configuration (`_roundSeconds`). With domain reload off, `Initialize` replaces the previous session's services and subscribers instead of reusing them.
- **Everything is released**: `Shutdown` saves pending data and clears the bus when the app quits, and the bootstrap subscribes to `Application.quitting` exactly once.
- **Screens are adapters.** Rules are in `GameRound`, so they run in an EditMode test without a scene.
- **Save data is protected.** A corrupt file puts the progress service in a protected state and shows a message instead of replacing the file.
- **Failures are visible.** A missing asset, a missing scene in the build, and an uninitialized service each log a clear error.

## Using the pattern in your game

Copy `GameServices`, `GameServicesBootstrap` and the service classes you need, then add your own services to `Initialize`. To use a container instead, see the [Pretzel VContainer](../../../com.uee03.pretzel.vcontainer/README.md) package and the Space Invaders example.
