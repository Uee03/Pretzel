# Parallax Example

A character runs to the right while the camera follows. Six layers scroll at different speeds and repeat forever.

| Scene | Camera | Layers |
|---|---|---|
| `Scenes/Parallax2D` | Orthographic | *Manual* factors: sky 0.95, mountains 0.8, hills 0.55, trees 0.3, ground 0, foreground trees -0.6 |
| `Scenes/Parallax25D` | Perspective | *Depth* mode. Layers sit at different Z values, built larger the farther they are, so the picture matches the 2D scene and the projection does the parallax |

Open a scene and press Play.

## Where things are

| File | Role |
|---|---|
| `Runtime/SampleRunner.cs` | Moves the character right with a small hop |
| `Runtime/SampleCameraFollow.cs` | A smoothed follow. It runs before the controller (order 300). With Cinemachine the Brain does this |
| `Editor/ParallaxSampleBuilder.cs` | Imports the art as tiled-ready sprites and builds both scenes. Run it from *Pretzel > Samples > Parallax > Build Scenes And Assets* |
| `Art/*.png` | The layers, drawn with VectorCraft. The generator and sources are in the package's `Art~` folder |

## Things to try

- Select a layer and change its *Factor* while the game runs, then call *Rebase* on the controller (right-click the component in the Inspector) to see the new value.
- Change the controller's *Focal Distance* in the 2.5D scene.
- Switch the 2.5D camera to Orthographic and press *Rebase*: the Depth layers now get their factor from the distance. They keep their sizes, so the layers no longer look the same size, which is why a perspective camera is the natural fit for this scene.
