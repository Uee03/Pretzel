# Pretzel Parallax

Parallax scrolling for 2D and 2.5D games. It needs no other Pretzel module and no third-party package, and it works with any camera, including Cinemachine.

- **`ParallaxLayer`** holds one layer's settings: how much it follows the camera, whether it repeats, and whether it snaps to pixels.
- **`ParallaxController`** moves every layer from one `LateUpdate`, after the camera has moved, without allocating. It does nothing in a frame where the camera stood still.

A layer's position is a function of how far the camera has moved from where it started, so there is no per-frame state to drift. A camera cut, a teleport or a disabled controller never leaves a layer in the wrong place.

## Set up

1. Add a **ParallaxController** (or use *GameObject > Pretzel > Parallax > Parallax Background*, which makes one with two layers).
2. Put a **ParallaxLayer** on each background object. They can be children of the controller or listed on it.
3. Leave *Camera* empty to use the main camera, or assign the real camera. With Cinemachine assign the camera that has the Brain, not a virtual camera.
4. Place each layer where it should be when the camera is at its starting position.

```csharp
ParallaxLayer mountains = mountainsObject.AddComponent<ParallaxLayer>();
mountains.Configure(new Vector2(0.8f, 0.8f), repeatX: true);
controller.Rebase();   // after changing layers, the camera type or the focal distance
```

## Factor

The factor is how much a layer follows the camera on each axis.

| Factor | The layer | Looks like |
|---|---|---|
| 0 | stays in the world | the play field, the ground |
| 0.3 | follows a little | trees behind the play field |
| 0.8 | follows a lot | distant mountains |
| 1 | stays on screen | the sky |
| below 0 | moves faster than the world | foreground in front of the play field |

## 2D and 2.5D

- **2D, orthographic camera.** Use *Manual* mode and choose factors by eye. This is the common case.
- **2.5D, perspective camera.** Layers placed at different depths already move at different speeds because of the projection, so nothing needs to be faked. Use *Depth* mode so the controller leaves them alone (a factor of 0) while still tiling them, or use *Manual* for a layer that should stay on screen, such as the sky.
- **2.5D, orthographic camera.** Use *Depth* mode. The factor comes from the layer's distance in front of the camera, so layers at different Z values show the parallax a perspective camera would. The controller's *Focal Distance* is the distance at which a layer does not move. Layers nearer than that move faster than the world, layers farther away follow the camera.

The sample has a 2D scene and a 2.5D scene built from the same art.

## Repeating layers

Turn on *Repeat X* (or *Repeat Y*) and the layer is shifted by whole tiles so it always stays within half a tile of the camera. Give the object a `SpriteRenderer` with **Draw Mode: Tiled**, a width of at least three tiles plus the width of the view, and import the sprite with **Mesh Type: Full Rect**. The tile size is read from the sprite, or set *Tile Size* yourself.

## Auto-scrolling backdrops

A camera that does not move still wants a moving backdrop: a vertical shooter, a runner with a fixed camera, a menu. Give a layer an **Auto Scroll** (world units per second) and the controller advances it every frame, on top of any camera parallax:

```csharp
starsFar.SetAutoScroll(new Vector2(0f, -0.4f));
starsNear.SetAutoScroll(new Vector2(0f, -1.6f));   // nearer layers scroll faster
controller.SetAutoScrollScale(1.5f);               // multiplies every layer, for example as a game speeds up
```

Auto scroll uses scaled time, so a backdrop stops with the game and slows with slow motion. On a repeating axis the scroll offset is kept within one tile, so the numbers never grow. The controller only skips its update when the camera is still **and** no layer scrolls.

## Pixel art

Set the controller's *Pixels Per Unit* and turn on *Snap To Pixels* on the layers that need it. Positions then land on whole pixels and the art does not shimmer.

## Cinemachine

The controller runs at execution order 300. The Cinemachine Brain has no explicit order (the default, 0) and moves the camera in `LateUpdate` (or after physics, for physics targets), so by the time the controller runs the camera is already in its final position for the frame. Assign the camera the Brain drives. Nothing else is needed.

## Performance

One `LateUpdate` for any number of layers, no allocations, no `GetComponent` or `Find` calls after `Rebase`, and an early exit when the camera did not move.

## Sample

**Parallax Example** (Package Manager > Samples) is a running character on a six-layer background: sky, mountains, hills, trees, ground and a foreground row, in a 2D scene (Manual factors, orthographic) and a 2.5D scene (Depth mode, perspective). The art was drawn with VectorCraft; the generator and the sources are in `Art~`. Run **Pretzel > Samples > Parallax > Build Scenes And Assets** to rebuild the scenes after changing the art.
