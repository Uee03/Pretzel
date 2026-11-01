# Pretzel VFX

Game feel for visual effects: the small things that make a hit land. It needs no other Pretzel module and no third-party package.

| Piece | What it does |
|---|---|
| `TimeScaleService` | Hit stop and slow motion on `Time.timeScale`, from a stack of requests that expire on their own |
| `SpriteFlash` | Flashes the tint of one or more sprites and restores the exact original colours |
| `PunchScale` | A squash-and-stretch pop on an object's scale that settles to exactly the original |
| `ITimeScale` | What game code depends on, so a test can pass a fake |
| `TimeScaleStack`, `FeelMath` | The logic as plain C#, unit tested |

```csharp
TimeScaleService.Instance.Freeze(0.06f);               // hit stop
TimeScaleService.Instance.SlowMotion(0.25f, 1f);       // slow motion for one unscaled second
spriteFlash.Flash();                                    // the colour and duration set in the Inspector
punchScale.Punch(0.35f, 0.3f);                          // 35 % bigger, over 0.3 seconds
```

## Hit stop and slow motion

`TimeScaleService` keeps a stack of requests. The time scale is the **slowest** among the live ones, so a hit stop during a slow motion does the sensible thing, and every request ends on its own after its **unscaled** duration. Nothing has to remember to put the time back.

- It cooperates with a pause. If another system changes `Time.timeScale` while a request is live, such as a pause menu, that value becomes the new baseline. A hit stop never un-pauses the game, and when it ends the time returns to what it was.
- A frozen game runs at `TimeScaleService.MinimumScale` (0.0001), not exactly 0. It looks and behaves frozen, and it is what lets the service tell its own freeze from a pause set to 0.
- The service runs from `Update` on unscaled time, so it keeps working while the time scale is almost 0. It is created on first use and kept for the session; `CancelAll()` gives the time back at once.
- Typical numbers: a light hit 0.03 to 0.05 s, a heavy hit 0.08 to 0.12 s, a slow motion of 0.2 to 0.4 for a second.

## Flash and punch

`SpriteFlash` and `PunchScale` run only while an effect plays: when idle they are disabled, so they cost nothing per frame. Both capture the original colour or scale at the **start** of an effect, so an effect fired during an effect does not lock in a half-flashed tint or a grown scale, and both restore the exact original values when they end or when the component is disabled. They run on unscaled time, so a hit stop does not freeze them.

A tint multiplies the sprite, so `SpriteFlash` suits red hit flashes and colour tints. A flash toward white needs a shader with a fill or additive colour: build one with Shader Graph and drive its colour property in the same way.

## Together

A hit is usually several effects at once. Write that in one place:

```csharp
private void OnHit()
{
    _time.Freeze(0.06f);
    _flash.Flash();
    _punch.Punch(0.35f, 0.3f);
    _shaker.AddTrauma(0.4f);   // ICameraShaker from Pretzel Camera
    _sfx.Play("hit_light");    // ISfxPlayer from Pretzel SFX
}
```

Each module is optional and none depends on another, so this composition belongs to your game. The SFX and Camera packages work the same way.

## Particles and VFX Graph

Particle systems and VFX Graph effects are content, not code, so they stay in your project. Pool them with Pretzel Pooling so a burst does not allocate.

## Sample

**Game Feel Example** (Package Manager > Samples) is a bouncing face with a button for each effect and one that fires them together. The *Time scale* line shows the hit stop at work. The art was drawn with VectorCraft; the generator is in `Art~`. **Pretzel > Samples > VFX > Build Scene And Assets** rebuilds the scene.
