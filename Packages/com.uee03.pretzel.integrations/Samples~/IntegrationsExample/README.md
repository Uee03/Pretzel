# Integrations Example

One MonoBehaviour per library, plus one that uses all three. Each folder has its own assembly that compiles only when its libraries are installed, so you can import the sample with any subset.

| Folder | Component | Needs | What it shows |
| --- | --- | --- | --- |
| `LitMotion` | `LitMotionDemo` | LitMotion | A tween whose ease is picked from Pretzel's `EaseType`, with `WithEase(EaseType)` and `ToLitMotionEase()` |
| `UniTask` | `UniTaskDemo` | UniTask | Every `ApiClient` UniTask adapter: parallel GETs with `UniTask.WhenAll`, POSTs, a file download, an optional texture download, and cancellation |
| `R3` | `R3Demo` | R3 (and its NuGet package) | Click counter, debounced text field, a request that switches on each click, and zipped GET and POST streams |
| `Combined` | `CombinedDemo` | all three | A click starts a request and the result slides in |

## Run a demo

1. Install the libraries you want from the Addons Extra tab.
2. Create a GameObject with a `UIDocument` and assign a `PanelSettings` asset. The document's source asset can stay empty.
3. Add the demo component to the same GameObject and enter Play mode.

The UniTask, R3 and Combined demos call `https://jsonplaceholder.typicode.com/`.
