# Pretzel UI

Runtime UI Toolkit helpers. Depends on `pretzel.core` and `pretzel.tween`. For UGUI, see `pretzel.ui.ugui`.

## Element factories

`UIToolkitUtility.CreateContainer`, `CreateButton`, `CreateLabel`, `CreateTextField`, `CreateSlider`, `CreateToggle`, `CreateListView` and about 40 more create a control, apply optional settings and add it to a parent in one call. `CreateElement<T>` is the generic form.

## Fading

```csharp
panel.FadeIn(0.4f, EaseType.OutCubic);
panel.FadeOut(0.4f, EaseType.InCubic, () => Debug.Log("hidden"));
```

- Fades use USS transitions on `opacity`. A fade-out ends with `display: none`, and a fade-in sets `display: flex` first.
- Starting a new fade replaces the running one. The replaced fade's callback is not called.
- `element.GetFader()` returns the `UIToolkitFader` of an element. Set `HideOnFadeOut` to false to keep a faded-out element receiving pointer events.
- UI Toolkit has fewer easing modes than `EaseType`. Quad maps to `EaseIn`, `EaseOut` and `EaseInOut`, and Quart, Quint and Expo map to the Cubic modes. See `UIEaseExtensions.ToEasingMode`.

## Components

- `UIDocumentFader`: fades a named element (or the root) of a `UIDocument`, optionally on start.
- `UIHoverFade`: shows or hides an element while another element is hovered.
- `UIControllerBase`: a screen controller next to a `UIDocument`. Override `OnBind` and `OnUnbind`. `Show`, `Hide`, `ShowDelayed`, `HideDelayed` and `SwitchTo` use `display`.

## Sample

`Samples~/UIUsage`: a controller that builds a panel and fades it with the extension methods.
