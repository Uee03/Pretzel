# Integrations Example

One MonoBehaviour per library, plus one that uses all three. Each folder has its own assembly that compiles only when its libraries are installed, so you can import the sample with any subset.

| Folder | Component | Needs | What it shows |
| --- | --- | --- | --- |
| `LitMotion` | `LitMotionDemo` | LitMotion | A tween whose ease is picked from Pretzel's `EaseType` |
| `UniTask` | `UniTaskDemo` | UniTask | Two `ApiClient` calls in parallel with `UniTask.WhenAll`, and cancellation |
| `R3` | `R3Demo` | R3 (and its NuGet package) | Click counter, debounced text field and a request that switches on each click |
| `Combined` | `CombinedDemo` | all three | A click starts a request and the result slides in |

## Run a demo

1. Install the libraries you want from the Addons Extra tab.
2. Create a GameObject with a `UIDocument` and assign a `PanelSettings` asset. The document's source asset can stay empty.
3. Add the demo component to the same GameObject and enter Play mode.

The UniTask, R3 and Combined demos call `https://jsonplaceholder.typicode.com/`.
