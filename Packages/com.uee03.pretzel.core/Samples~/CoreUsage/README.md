# Core Usage

| File | What it shows |
|------|---------------|
| `CoreUsageExample.cs` | Every `Guard` check, and how to use `Instance`, `HasInstance` and `TryGetInstance` safely. |
| `SceneDirector.cs` | `Singleton<T>`: lives and dies with its scene. A duplicate in the same scene destroys itself. |
| `GameSession.cs` | `PersistentSingleton<T>`: survives scene loads. The first instance wins. |
| `MusicPlayer.cs` | `RegulatorSingleton<T>`: survives scene loads. The newest instance wins, and `InitializationTime` tells them apart. |

## Try it

1. Add `CoreUsageExample` to a GameObject in an empty scene.
2. Press Play. `GameSession` is created on first use of `GameSession.Instance`.
3. Add `SceneDirector` or `MusicPlayer` to the scene to see the other messages.

Rules:

- Call `base.Awake()` (and `base.OnDestroy()`) when you override them.
- In `OnDestroy` and other teardown code, use `HasInstance` or `TryGetInstance()`. `Instance` would create a new
  object while the scene is closing.
- Validate input at a boundary with `Guard`, and catch `ArgumentException` there if the input comes from a player or a file.
