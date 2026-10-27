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
minimum duration, activate, set the active scene (additive), optionally unload unused assets, hide the transition.

- One load at a time; `IsBusy` reports it and a second call throws `InvalidOperationException`.
- A scene missing from the build profile throws `ArgumentException`, and the transition still hides.
- Unity cannot abort a scene load. Cancelling before loading starts throws `OperationCanceledException`. Cancelling
  later unloads an additive scene again and throws, and lets a single-mode load finish.
- `SceneLoaded` is raised after each successful load.

## UIDocumentSceneTransition

A fading loading screen from a `UIDocument`. Give the UXML a full-screen root with a background and, optionally, a
`ProgressBar` named `progress`. It starts hidden, moves itself to DontDestroyOnLoad (so single-mode loads do not destroy
it), and uses the Pretzel UI fader with an `EaseType`. Implement `ISceneTransition` for any other screen (UGUI,
a camera fade, a shader wipe).

## Sample

`Samples~/ScenesUsage`: a loading screen and a menu with single and additive loads.
