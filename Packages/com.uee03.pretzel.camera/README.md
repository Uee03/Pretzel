# Pretzel Camera

Camera tools on [Cinemachine 3](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/index.html) for 2D, 2.5D and 3D games. Cinemachine does the follow, the blends and the confiners; this package adds what a game needs on top, as Cinemachine extensions that run at the end of the pipeline and never fight it.

| Piece | What it does |
|---|---|
| `CameraShake` | Trauma-based screen shake: stack hits, it decays by itself, strength is trauma squared |
| `PlayfieldFitter` | Keeps a playfield visible on every aspect ratio, for orthographic and perspective cameras |
| `TraumaShake`, `PlayfieldFit` | The same logic as plain C#, for your own cameras and for tests |
| `ICameraShaker` | What effects code depends on, so it does not care which camera shakes |
| *GameObject > Pretzel > Camera* | Builds a ready 2D, 2.5D or 3D rig: camera, Brain, CinemachineCamera, composer and shake |

The package depends on `com.unity.cinemachine` 3.1.7. Cinemachine is used for everything it already does well; a custom camera is only written where it is the better tool.

## Set up

Select your player and choose **GameObject > Pretzel > Camera > 2D Camera Rig** (or 2.5D, or 3D). The rig has:

- a main camera with a `CinemachineBrain` (an existing main camera is reused),
- a `CinemachineCamera` following the selected object,
- the right body for the dimension (see below),
- a `CameraShake`, and for 2D a `PlayfieldFitter` set to 16 x 9.

| Rig | Camera | Body |
|---|---|---|
| 2D | Orthographic | `CinemachinePositionComposer` with damping and a dead zone. Add a `CinemachineConfiner2D` to keep the view inside a level |
| 2.5D | Perspective at a fixed distance from the gameplay plane | `CinemachinePositionComposer` at distance 12, so the camera slides along the plane and the depth layers show real parallax |
| 3D | Perspective | `CinemachineOrbitalFollow` and `CinemachineRotationComposer` |

The same rigs are available from code in the Editor with `CameraRigBuilder.Create(CameraRigKind.TwoD, target)`.

## Screen shake

```csharp
[SerializeField] private CameraShake _shake;

_shake.AddTrauma(0.25f);   // a tap
_shake.AddTrauma(0.6f);    // a big hit
_shake.AddTraumaUpTo(0.1f, 0.25f);   // a rumble that never drowns out a hit
```

Trauma is between 0 and 1 and stacks (it never goes past 1). The shake strength is trauma squared, so a small hit is a flick, two hits are noticeably bigger, and a big hit is violent. Trauma decays on its own at *Decay Per Second*; the shake ends exactly at rest.

- The offset is added at the end of the Cinemachine pipeline, in the camera's own axes, so it composes with follow, confiners and blends.
- It runs on **unscaled time** by default, so it keeps decaying during hit stop and pauses instead of freezing the camera off-centre.
- Cost: three noise samples a frame while shaking, nothing allocated, and nothing at all while still.

Depend on `ICameraShaker` in game code if you do not want the code to reference Cinemachine:

```csharp
public sealed class Explosion
{
    private readonly ICameraShaker _shaker;
    public Explosion(ICameraShaker shaker) => _shaker = shaker;
    public void Detonate() => _shaker.AddTrauma(0.7f);
}
```

An assembly that uses `CameraShake` or `PlayfieldFitter` directly needs a reference to `Unity.Cinemachine`, because both derive from `CinemachineExtension`.

For continuous handheld motion or a shake that depends on where an explosion is, Cinemachine's own noise profiles and Impulse system are the right tools; use them next to this.

## Playfield fit

Give `PlayfieldFitter` the area that must stay visible, and the lens follows the screen: you never get cropped gameplay on a narrow phone or a wasted view on an ultrawide.

| Mode | Result |
|---|---|
| Contain | The whole playfield is visible. Extra space shows around it |
| Cover | The playfield fills the screen and part of it is cropped |
| Width | The playfield's width fits exactly |
| Height | The playfield's height fits exactly |

For perspective cameras set *Distance* to the camera's distance from the playfield's plane, and the field of view is computed. *Padding* adds world units around the area. The fitter sets the lens before the Cinemachine pipeline runs, so a confiner or composer already sees the final size. Physical cameras are left alone.

## Performance

Both extensions do arithmetic only: no allocation, no `GetComponent` or `Find`, no per-frame logging. A still camera costs one early return.

## Sample

**Camera Example** (Package Manager > Samples) is a 32 x 18 arena with an orb that wanders, a follow camera, buttons that add trauma, and a button that cycles the fit mode. Resize the Game view to watch the fit. The art was drawn with VectorCraft; the generator is in `Art~`. Run **Pretzel > Samples > Camera > Build Scene And Assets** to rebuild the scene.
