# Level Loading

Loads levels additively behind a loading screen, then spawns each level's enemies from pools while the screen is still
up. The progress bar covers both phases: the scene file (40 % by default) and the level setup (the rest).

Needs **Pretzel Pooling**. Install it from the Setup window's Addons tab; until then this sample's assembly is skipped.

| File | What it shows |
|------|---------------|
| `LevelLoader.cs` | `SceneLoader` with `SetupAsync`, `SceneProgressWeight`, `LoadingStatus`, a minimum duration, and unloading the previous level behind the screen. |
| `LevelSetup.cs` | Setup work inside the level: a `PoolFactory`, spawning from markers with `SubProgress` and `FrameBudget`, and status lines. |
| `SpawnMarker.cs` | A placed marker with an enemy kind and count, drawn as gizmos. Designers place markers, not enemies. |
| `Enemy.cs` | A pooled enemy that resets in `OnTakenFromPool`. |
| `LevelLoadingScreen.uxml` | The loading screen: a `status` label and a `progress` bar with a percentage. |

## Try it

1. **Bootstrap scene.** Create a scene named `Bootstrap` with:
   - a camera and a light that stay loaded,
   - a GameObject with a `UIDocument` (assign a PanelSettings with a high sort order and `LevelLoadingScreen.uxml`)
     and `UIDocumentSceneTransition`,
   - a GameObject with `LevelLoader`, with the transition assigned.
2. **Enemy prefabs.** Make two prefabs, for example a capsule and a cube, and add `Enemy` to both.
3. **Level scenes.** Create scenes named `LevelA` and `LevelB`. In each, add a ground plane and a GameObject with
   `LevelSetup`. Under **Enemies > Entries**, add a `Grunt` and a `Brute` entry with the prefabs. Add a few child
   objects with `SpawnMarker`, and raise **Count** on some of them.
4. Add `Bootstrap`, `LevelA` and `LevelB` to the scene list of the build profile, open `Bootstrap` and press Play.
   `LevelA` loads behind the screen. Use **Load Next Level** from the `LevelLoader` context menu to switch levels.

**Demo Delay Per Enemy** on `LevelSetup` slows spawning down so the second half of the bar is visible. Set it to 0 in a
real game; spawning then uses the frame budget only.

## What happens during a load

```
show screen -> load scene file (0-40 %) -> activate -> unload previous level -> pools -> spawn (40-100 %) -> settle -> hide
```

Rules:

- Keep the object that awaits the load in a scene that survives it (here `Bootstrap`), or use a single-mode load with
  a persistent loader. Otherwise its cancellation token fires when the old scene goes.
- Do heavy setup in `SetupAsync`, not in `Awake` or `Start` of the level, so it happens behind the screen and moves
  the bar.
- Spread work over frames (`FrameBudget`, or `Awaitable.NextFrameAsync`). A long synchronous loop freezes the screen
  and the bar.
- Cancelling or failing during setup unloads an additive level, so a half-prepared level never stays loaded.
