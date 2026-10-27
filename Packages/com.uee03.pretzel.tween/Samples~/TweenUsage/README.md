# Tween Usage

1. Create a cube and add `TweenUsageExample`.
2. Press Play. The cube moves with the chosen `EaseType`, pulses with an `AnimationCurve`, squashes with a
   `Vector2` lerp and changes colour with a `Color` lerp. The Console prints `Evaluate` for every ease type.
3. Turn on **Use Curve** to drive every lerp with the curve instead.

Notes:

- `EaseUtility.Lerp` is unclamped, so Back and Elastic overshoot the target.
- `Evaluate` and the eased `Lerp` take `period` before `amplitude`. The individual elastic functions, such as
  `OutElastic(t, amplitude, period)`, take `amplitude` first.
- The colour uses a `MaterialPropertyBlock`, so no material copy is created.
