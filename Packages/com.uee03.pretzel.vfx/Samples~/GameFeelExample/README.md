# Game Feel Example

A face bounces around the view on scaled time, so a hit stop or a slow motion is easy to see. Open `Scenes/GameFeelExample` and press Play.

| Button | What it does |
|---|---|
| **Hit stop** | `TimeScaleService.Instance.Freeze(0.08f)`: the face stops for a moment |
| **Slow motion** | `SlowMotion(0.25f, 1f)`: a quarter speed for one second |
| **Flash** | `SpriteFlash.Flash()`: a red tint that fades out |
| **Punch** | `PunchScale.Punch()`: a pop that settles |
| **All together** | A 0.06 s freeze, a flash and a bigger punch at once, as game code would on a hit |

The line under the buttons shows `Time.timeScale`. During a hit stop it reads 0.0001, not 0 (see the package README for why). Press **Hit stop** and **Slow motion** together to see that the slowest request wins.

| File | Role |
|---|---|
| `Runtime/SampleBouncer.cs` | Moves the face on scaled time |
| `Runtime/FeelPanel.cs` | The UI Toolkit buttons |
| `Editor/GameFeelSampleBuilder.cs` | Imports the art and builds the scene. *Pretzel > Samples > VFX > Build Scene And Assets* |
| `Art/face.png` | Drawn with VectorCraft. The generator is in the package's `Art~` folder |
