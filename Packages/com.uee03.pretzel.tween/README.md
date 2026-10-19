# Pretzel Tween

Easing functions for `float`, `Vector2`, `Vector3` and `Color`. Depends on `pretzel.core`.

```csharp
float t = EaseUtility.Evaluate(EaseType.OutBack, time);
Vector3 p = EaseUtility.Lerp(EaseType.InOutCubic, from, to, time);
Color c = EaseUtility.Lerp(from, to, time, myAnimationCurve);
```

- `EaseType` lists 31 curves: Linear plus In, Out and InOut of Sine, Quad, Cubic, Quart, Quint, Expo, Circ, Back, Elastic and Bounce.
- `Evaluate` clamps `time` to 0 to 1. `Lerp` is unclamped, so overshoot curves such as `OutBack` can pass the target.
- Elastic curves take `period` and `amplitude`. Amplitude below 1 is raised to 1.
- Every curve is exact at its endpoints, and `InOutBack` uses the standard 1.525 constant.

To drive LitMotion with the same `EaseType`, install `pretzel.integrations` and add LitMotion.

## Sample

`Samples~/TweenUsage`.
