# Pretzel UI (UGUI)

UGUI components. Depends on `pretzel.core`, `pretzel.ui`, `pretzel.tween` and `com.unity.ugui`. For UI Toolkit, see `pretzel.ui`.

- `UIFader`: fades a `CanvasGroup`, or a list of `CanvasRenderer`s. Methods: `FadeIn`, `FadeOut`, `FadeInNow`, `FadeOutNow`. Pick an `EaseType` or an `AnimationCurve` per direction, and fade on start. It finds a `CanvasGroup` on the same object when none is assigned. Uses `FadeType` from `pretzel.ui`.
- `OnMouseHoverFade`: fades a `UIFader` while the pointer is over the object. Works on UI graphics and on colliders.
- `UGUIExtensions`: `Button.AddListener`, `RemoveListener`, `RemoveAllListeners` and `Graphic.SetPlaceHolderText` (does nothing unless the graphic is a `TMP_Text`).
- `UIFaderEditor`: the inspector for `UIFader`.

`UIFader` fields are private and serialized, so the old public fields from LazyJedi are gone. Set them in the Inspector.

## Sample

`Samples~/UGUIUsage`: a `UIFader` on a `CanvasGroup` with a hover fade and button extensions.
