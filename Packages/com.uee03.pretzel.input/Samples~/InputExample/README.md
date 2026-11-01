# Input Example

A square you move and jump with. Open `Scenes/InputExample` and press Play.

| Do this | What it shows |
|---|---|
| **A / D** or the left stick | `Move` in the `Gameplay` context |
| **Space** or gamepad A | `Jump`, buffered for 0.15 s: press it a moment before landing and the jump still happens |
| **Esc** or Start | Pushes the `Menu` context: the Gameplay map is disabled, so the square stops. Press it again to resume |
| **Rebind Jump** | A `RebindSession` on the keyboard binding; Esc cancels. A completed rebind is saved with `BindingPersistence` and applied again at the next start |
| **Reset bindings** | `BindingPersistence.Reset` |
| **Block input 2 s** | `InputBlockers.Block("example-block")`, what a scene load does: nothing responds, then the context returns by itself |

The line at the top shows the active context and the device kind used last. Use a gamepad and a keyboard in turn to watch it change.

| File | Role |
|---|---|
| `Runtime/InputExample.cs` | The composition root: router, device tracker, binding store, and the pause and resume actions |
| `Runtime/ExamplePlayer.cs` | Moves and jumps from actions only, with an `InputBuffer` |
| `Runtime/InputExamplePanel.cs` | The UI Toolkit readout and buttons |
| `Input/ExampleActions.inputactions` | Two maps named after the contexts: `Gameplay` (Move, Jump, Pause) and `Menu` (Resume) |
| `Editor/InputSampleBuilder.cs` | Builds the actions asset, the sprite and the scene. *Pretzel > Samples > Input > Build Scene And Assets* |

The example works on a copy of the actions asset, so rebinding never changes the asset on disk. Rebinds are kept in PlayerPrefs under `pretzel.input.example`.
