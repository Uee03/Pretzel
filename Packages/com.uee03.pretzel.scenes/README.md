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

- One load at a time; `IsBusy` reports it and a second call throws `InvalidOperationException`.
- A scene missing from the build profile throws `ArgumentException`, and the transition still hides.
- Unity cannot abort a scene load. Cancelling before loading starts throws `OperationCanceledException`. Cancelling
  later unloads an additive scene again and throws, and lets a single-mode load finish.
- `SceneLoaded` is raised after each successful load, after setup.
- `Progress` and the transition receive one value from 0 to 1 for the whole load. It never goes backwards.

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

- `SceneSetupContext` carries the loaded `Scene`, a `Progress` for the setup phase (0 to 1), the `CancellationToken`,
  `ReportStatus` and `FindInScene<T>`, which searches only that scene.
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
