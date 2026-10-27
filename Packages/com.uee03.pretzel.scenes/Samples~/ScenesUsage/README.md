# Scenes Usage

| File | What it shows |
|------|---------------|
| `SceneMenu.cs` | `SceneLoader` with single and additive loads, a minimum duration, unloading unused assets and cancellation tied to the component. |
| `LoadingScreen.uxml` | A full-screen loading screen with a `ProgressBar` named `progress`. |

## Try it

1. Add two scenes to the scene list of the build profile, for example `Level1` and `Level1Lighting`.
2. Create a GameObject with a `UIDocument` (assign a PanelSettings with a sort order above your game UI and
   `LoadingScreen.uxml`) and add `UIDocumentSceneTransition`. It moves itself to DontDestroyOnLoad.
3. Add `SceneMenu` to another GameObject, assign the transition and the scene names.
4. Press Play and use the component's context menu.

Rules:

- One load at a time. `IsBusy` tells you when one is running.
- Unity cannot abort a scene load. Cancelling after loading started unloads an additive scene again; a single-mode
  load always finishes. Code in the old scene is destroyed by a single-mode load, so pass a token tied to an object
  that survives, or expect `OperationCanceledException`.
- Scene activation is held at 90 % until the transition is fully shown and the minimum duration has passed, so
  `Awake` and `Start` of the new scene never run behind a half-faded screen.
