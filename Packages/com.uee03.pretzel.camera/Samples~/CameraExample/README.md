# Camera Example

A 32 x 18 arena, an orb that wanders through it, and a Cinemachine 2D rig made with *GameObject > Pretzel > Camera > 2D Camera Rig*. The camera follows the orb with damping and shows a 16 x 9 playfield on every aspect ratio.

Open `Scenes/CameraExample` and press Play.

| Control | What it shows |
|---|---|
| **Tap**, **Hit**, **Boom** | `CameraShake.AddTrauma` with 0.25, 0.5 and 0.9. Click Hit several times quickly: trauma stacks but never exceeds 1 |
| **Rumble** | `AddTraumaUpTo(0.08, 0.25)` every 60 ms: a continuous low shake that never drowns out a hit |
| **Fit** | Cycles `PlayfieldFitter` through Contain, Cover, Width and Height. Resize the Game view, or pick a different aspect ratio, to see the difference |

The line under the buttons shows the aspect ratio, the orthographic size the fitter chose, and the current trauma.

| File | Role |
|---|---|
| `Runtime/SampleWanderer.cs` | Moves the orb along a looping path |
| `Runtime/CameraDemoPanel.cs` | The UI Toolkit buttons |
| `Editor/CameraSampleBuilder.cs` | Imports the art and builds the scene with `CameraRigBuilder`. *Pretzel > Samples > Camera > Build Scene And Assets* |
| `Art/*.png` | Drawn with VectorCraft. The generator is in the package's `Art~` folder |
