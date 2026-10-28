# Pretzel Scenes

Smooth scene loading on `SceneManager` and `Awaitable`. Depends on Pretzel Core, Tween and UI.

```csharp
SceneLoader loader = new();
Scene level = await loader.LoadAsync("Level1", new SceneLoadOptions
{
    Mode = LoadSceneMode.Additive,
    Transition = loadingScreen,          // any ISceneTransition
    MinimumDuration = 1f,                // real-time seconds, avoids a flashing loading screen
    Progress = new Progress<float>(p => Debug.Log(p))
}, destroyCancellationToken);

await loader.UnloadAsync(level);
```

Order of a load: show the transition, load to 90 % with activation held back while reporting progress, wait for the
minimum duration, activate, set the active scene (additive), run `SetupAsync`, optionally unload unused assets, wait
`SettleFrames`, hide the transition.

- `Progress` and the transition receive one value from 0 to 1 for the whole load. It never goes backwards.
- A scene missing from the build profile throws `ArgumentException`, and the transition still hides.
- Unity cannot abort a scene load. Cancelling throws `OperationCanceledException` once the scene that is loading has
  activated. An additive load then unloads what it opened; a single-mode load keeps its scene, because the previous
  scenes are already gone.
- Any failure or cancellation of an additive load unloads the scenes it opened, so a half prepared level never stays.
- `LoadingPriority` (default `High`) sets `Application.backgroundLoadingPriority` during the load and restores it after.
  `null` leaves it alone.

## Requests during a load

One load runs at a time. `RequestPolicy` decides what happens to a request that arrives meanwhile:

| Policy | Behaviour |
|--------|-----------|
| `Queue` (default) | Waits and runs in order. A waiter whose token is cancelled leaves the queue. |
| `Ignore` | Returns at once with an invalid `Scene` (an empty list for groups, `null` for a preload). Suits double clicks. |
| `Throw` | Throws `InvalidOperationException`. |

## State, events and reports

- `State` (`SceneLoadState`: Idle, ShowingTransition, Loading, Preloaded, Activating, SettingUp, HidingTransition),
  `IsBusy` and `Progress`, with `StateChanged` and `ProgressChanged`.
- `LoadStarted` fires before the transition shows; `LoadFinished` fires after every load, successful or not, once the
  transition is hidden. Use them to block gameplay input, pause, or duck audio for the duration of a load.
- `SceneLoaded` fires for each scene of a successful load, after setup.
- `SceneLoadReport` (passed to both events and kept in `LastReport`) has the scene names, the mode, timings per phase
  (show, load, activate, setup, hide, total) and the outcome: `Succeeded`, `Cancelled`, `Error`, `RecoveredToFallback`.
  Log it or send it to analytics.
- An exception thrown by a listener is logged and does not break the load.

## Scene groups

A `SceneGroup` asset (Create > Pretzel > Scenes > Scene Group) lists scenes that form one level, such as gameplay,
environment, lighting and audio, and names the active one. In the Editor it warns about scenes missing from the build
profile.

```csharp
IReadOnlyList<Scene> scenes = await loader.LoadGroupAsync(forestGroup, options, destroyCancellationToken);
await loader.UnloadGroupAsync(forestGroup);
```

The scenes load and activate one after another under one transition and one progress bar; with a single-mode load the
first scene replaces the open scenes. `SceneSetupContext.Scenes` lists every scene of the load, and `FindInScene<T>`
searches the active scene first, then the others. `SceneGroup.Create` builds a group from code.

## Preloading

Load the next area in the background during gameplay and switch to it instantly, for example at a door:

```csharp
PreloadedScene next = await loader.PreloadAsync("Cave", progress: null, ThreadPriority.Low, destroyCancellationToken);
// later
Scene cave = await next.ActivateAsync(new SceneLoadOptions { SetupAsync = SpawnCaveAsync });
// or, if the player turns back
await next.CancelAsync();
```

- The scene is held at 90 % without a transition and activates additively; `ActivateAsync` runs the rest of a normal
  load (optional transition, setup, settle frames).
- While a preload is held Unity cannot finish any other asynchronous load, so the loader stays busy and queues other
  requests. Always activate or cancel a preload. `CancelAsync` activates the scene and unloads it, because Unity
  cannot abort a load.
- A handle can be used once; a second `ActivateAsync` or `CancelAsync` throws `InvalidOperationException`.

## Failure fallback

Set `FallbackScene` (for example the main menu) to recover from a failed load: the loader loads it in single mode
behind the transition and throws `SceneLoadException` with the original error as `InnerException`. Cancellation never
falls back.

## Setting up the level behind the loading screen

Bake fixed content (geometry, lighting, set pieces, spawn markers) into the scene. Spawn what depends on save data or
runs through pools after activation, while the screen still covers the scene, with `SetupAsync`:

```csharp
SceneLoadOptions options = new()
{
    Mode = LoadSceneMode.Additive,
    Transition = loadingScreen,
    SceneProgressWeight = 0.4f,              // scene file 0-40 %, setup 40-100 %
    LoadingStatus = "Loading the forest",
    SetupAsync = async context =>
    {
        LevelSetup level = context.FindInScene<LevelSetup>();
        context.ReportStatus("Spawning enemies");
        await level.SpawnAsync(new SubProgress(context.Progress, 0f, 0.8f), context.CancellationToken);
        context.ReportStatus("Warming up effects");
        await level.WarmUpAsync(new SubProgress(context.Progress, 0.8f, 1f), context.CancellationToken);
    }
};
```

- `SceneSetupContext` carries the active `Scene`, every opened scene in `Scenes`, a `Progress` for the setup phase
  (0 to 1), the `CancellationToken`, `ReportStatus` and `FindInScene<T>`, which searches only the scenes of this load.
- `SubProgress` maps 0 to 1 onto part of another progress; nest it to split a step further.
- Spread heavy work over frames with `FrameBudget` (Pretzel Core) or `Awaitable.NextFrameAsync`; a long synchronous
  loop freezes the screen and the bar.
- Cancelling or an exception during setup unloads an additive scene and rethrows. A single-mode scene stays loaded.
- `Scene.FindComponent<T>()` is also available as an extension method.

Keep the object that awaits the load in a scene that survives it, such as a bootstrap scene with additive levels.

## UIDocumentSceneTransition

A fading loading screen from a `UIDocument`. Give the UXML a full-screen root with a background and, optionally, a
`ProgressBar` named `progress` (its title shows the percentage) and a `Label` named `status` for `ReportStatus` lines.
It starts hidden, moves itself to DontDestroyOnLoad (so single-mode loads do not destroy
it), and uses the Pretzel UI fader with an `EaseType`. Implement `ISceneTransition` for any other screen (UGUI,
a camera fade, a shader wipe), and `ISceneStatusDisplay` if it can show status text.

## Samples

- `Samples~/ScenesUsage`: a loading screen and a menu with single and additive loads.
- `Samples~/LevelLoading`: a bootstrap scene that loads levels additively; each level spawns pooled enemies from placed
  markers behind the loading screen, with one progress bar for both phases. Needs Pretzel Pooling.
