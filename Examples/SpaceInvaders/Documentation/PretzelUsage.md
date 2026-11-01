# Where Pretzel is used

Every Pretzel module the game uses, what it does here, and where to read it. Paths are relative to `Assets/_Scripts/Runtime` unless they start with `_Project`. To find all uses of a module, search for its namespace, for example `using Pretzel.Pooling`.

## At a glance

| Module | Used for | Main files |
|---|---|---|
| **Setup** | Folder preset, the VContainer composition root, the Pretzel 2D scene template, a scriptable setup API | `Composition/*`, `_Project/Settings/Composition`, `Editor/SceneBuilder.cs` |
| **Integrations** | VContainer scopes and installers | `Composition/GameInstaller.cs`, `Gameplay/Level/LevelInstaller.cs`, `UI/TitleInstaller.cs` |
| **Core** | Timers, argument guards, a subscription bag | `Gameplay/Actors/*`, `Gameplay/Level/LevelPresenter.cs` |
| **Events** | The decoupling bus between actors, rules, HUD, audio and effects | `Gameplay/Messages/*`, all presenters |
| **Pooling** | Bullets, bombs, invaders and explosions | `ProjectileSpawner.cs`, `InvaderFormation.cs`, `ExplosionSpawner.cs` |
| **Scenes** | Loading the game behind a loading screen, with setup work | `Composition/GameNavigator.cs`, `Gameplay/Level/LevelSetup.cs` |
| **UI** | Views, screen stack, toasts, safe area | `UI/HudView.cs`, `UI/TitleView.cs` |
| **Audio** | Cues, pooled voices, channel volumes | `Audio/GameAudioReactions.cs`, `Composition/GameFlow.cs` |
| **Data** | Saving the high score | `Composition/SaveService.cs`, `GameService.cs`, `GameProgress.cs` |
| **Preferences** | The saved master volume | `Composition/GameFlow.cs`, `Audio/MasterVolumeSetting.cs` |
| **DevConsole** | In-game commands for debugging | `Gameplay/Level/DevCommands.cs` |
| **Tween** | Easing for the march tempo and explosions | `Gameplay/Rules/MarchTempo.cs`, `Gameplay/Actors/ExplosionFx.cs` |
| **Extensions** | A random element from a list | `Gameplay/Actors/InvaderFormation.cs` |
| **UI (Editor)** | The wave inspector | `Editor/WaveDefinitionEditor.cs` |

Modules the game does not need: Api, Cloud, Multiplayer, UI (UGUI) and the optional LitMotion, UniTask and R3 adapters. The Editor package is referenced for its utilities, but the game's code does not call it.

## Setup

- **Folder preset.** *Pretzel > Create > Folders* made the layout described in the README: scripts under `_Scripts`, everything else under `_Project`.
- **VContainer tab.** It generated `GameInstaller.Generated.cs`, `GameInstaller.cs`, `GameFlow.cs`, the starter `ISaveService`, `SaveService`, `IGameService`, `GameService`, `GameState` and `GameProgress`, and the `GameNavigator` service shell, then created the scope prefab, the installer asset and `VContainerSettings`. Only `GameInstaller.Generated.cs` is machine-owned. The saved choices are in `ProjectSettings/Pretzel/pretzel-settings.json`.
- **Scripted setup.** The same generation runs headless, which is how this project was set up: `Unity -batchmode -projectPath . -executeMethod Pretzel.Setup.PretzelSetupBatch.Composition` (do not pass `-quit`). See the Setup README.
- **Scene template.** `Editor/SceneBuilder.cs` creates both scenes from *Pretzel 2D*, so they start with the `[CAMERA]`, `[ENVIRONMENT]`, `[MANAGERS]` and `[UI]` groups.

## Integrations (VContainer)

- `GameInstaller` is a `ScriptableInstaller`: registrations kept in an asset that applies to the whole project.
- `LevelInstaller` and `TitleInstaller` are `MonoInstaller`s: registrations that need references to objects in one scene.
- The root scope prefab and both scene scopes are `PretzelLifetimeScope` components, which register the settings assets and run the installers.

## Core

- `Cooldown`: the player's fire rate (`PlayerShip.TryFire`) and the gap between invader bombs (`InvaderFormation.TryDropBomb`).
- `CountdownTimer`: the march step (`InvaderFormation`), the mystery ship's next appearance (`UfoSpawner`) and the ship's invulnerability after a hit (`PlayerShip`). The owner ticks it, so pausing (`Time.timeScale = 0`) needs no extra code.
- `FrameBudget`: spreads the pool prewarm over several frames (`InvaderFormation.PrewarmAsync`).
- `Guard.NotNull`: validates what `[Inject]` methods receive.
- `CompositeDisposable` and `AddTo`: one bag for each class's event subscriptions (`GameAudioReactions`, `HudPresenter`, `LevelPresenter`, `ExplosionSpawner`).

## Events

`EventBus` is registered as `IEventBus`. Messages are `readonly struct`s in `Gameplay/Messages/`. See the [event table](Architecture.md#the-event-bus) for who publishes and who listens.

## Pooling

| What | How | File |
|---|---|---|
| Bullets and bombs | `PoolFactory<ProjectileKind, Projectile>` as a serialized field, prefabs and prewarm set in the Inspector | `Gameplay/Actors/ProjectileSpawner.cs` |
| Invaders | `PoolFactory<InvaderKind, Invader>`, prewarmed behind the loading screen | `Gameplay/Actors/InvaderFormation.cs` |
| Explosions | `IPoolRegistry` from the container, one pool per prefab | `Gameplay/Actors/ExplosionSpawner.cs` |

`Invader`, `Projectile` and `ExplosionFx` implement `IPoolable` to reset themselves, and release through the `PooledObject` that the pool adds. Shooters depend on `IProjectileSpawner`, not on a prefab.

## Scenes

- `GameNavigator` builds a `SceneLoadOptions` with a transition, a minimum duration, `SceneProgressWeight`, a status line, a `SetupAsync` callback and a `FallbackScene`. The loader's `RequestPolicy` is `Ignore`, so a double click on Play does nothing.
- `LevelSetup.PrepareAsync` is the `SetupAsync` work: it reports status and prewarms the invaders with the loader's progress. `GameNavigator` finds it with `SceneSetupContext.FindInScene<T>()`.
- The loading screen is a `UIDocumentSceneTransition` on a child of the root scope prefab, built from `_Project/UI/Layouts/Loading.uxml` (a `status` label and a `progress` bar). It stays in place because its parent already survives scene loads.

## UI

- `HudView` and `TitleView` derive from `UIControllerBase`: they query elements in `OnBind` with `QRequired<T>`, hook buttons with `OnClick` and sliders with `OnValueChanged`, and release them in `OnUnbind`.
- `HudView` uses a `UIScreenStack` with modal pushes for the pause and game-over overlays, and a `ToastService` for wave and mystery-ship messages.
- The layouts use `SafeAreaElement`. The USS (`_Project/UI/Styles/SpaceInvaders.uss`) follows the BEM naming rules and also restyles `pretzel-toast` to show that your own rules override the toast default.
- Presenters (`HudPresenter`, `TitlePresenter`) hold the logic. The views only expose events and setters.

## Audio

`AudioService` is a child object of the root scope prefab (Setup adds it) and is registered as `IAudioService`. `SoundBank` holds six `AudioCue` assets and the four march clips. `GameAudioReactions` plays them from events, and `GameFlow` pushes the saved volumes to the `Master`, `Music`, `Sfx` and `UI` channels.

## Data and Preferences

- `SaveService` is the only class that touches `IDataStore` (`DataIO.Default`, plain JSON). A corrupt file throws and is kept, never overwritten.
- `GameService` loads `GameProgress` once through `LoadProgressAsync`, raises `ProgressLoaded` and saves the high score at the end of a game. Progress is not saved if loading failed.
- `GameFlow` is an `IAsyncStartable`: it loads preferences first, applies them to the audio channels, then loads progress. `MasterVolumeSetting` changes the master volume with `PreferencesService.Modify` and saves.

## DevConsole

`DeveloperConsole` is a component in the Game scene (back-quote to open). `DevCommands` registers `god`, `score`, `lives`, `wave`, `clear` and `ufo` on its `CommandRegistry`, and removes them when the level scope is disposed.

## Tween and Extensions

`MarchTempo` uses `EaseUtility.Lerp(EaseType.InQuad, ...)` so the march speeds up faster as the formation thins out. `ExplosionFx` grows with `OutCubic` and fades with `InQuad`. `InvaderFormation` picks the bomber with `GetRandomItem()`.

## Editor tooling

`WaveDefinitionEditor` derives from `InspectorBase` and uses `EditorUI` (`Section`, `Row`, `Help`, `Separator`) for a live formation preview and warnings. The builder under `Editor/` is covered in [Build it yourself](BuildItYourself.md#editor-tools).

## Things worth knowing

- **There is no Pretzel input module.** `Gameplay/Actors/ShipControls.cs` reads Unity's project-wide Input System actions, which the template already assigns.
- **The object that awaits a scene load must outlive it.** That is why `GameNavigator` is in the root scope rather than in a scene.
- **Scene components get dependencies by method injection.** An `[Inject]` method runs when the scope builds, before the component's `Awake`, so these methods only store what they receive.
- **Starter code is yours.** The generated services were edited freely; the only file Setup rewrites is `GameInstaller.Generated.cs`.
- **Play Mode keeps the template's default of no domain reload.** The game therefore holds no mutable static state: everything lives in scopes and components, so each Play session starts clean.
