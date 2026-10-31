# Floating Text

Damage numbers on a UI Toolkit panel, with pooled labels. The same idea works for toasts, chat lines, combat logs and
any other element that appears and disappears many times a second.

| File | What it shows |
|------|---------------|
| `FloatingTextLayer.cs` | `UnityEngine.Pool.ObjectPool<Label>` for the elements and `ObjectPool<Entry>` for the animation state. Elements are hidden with `display: none` on release, animated with `translate` and `opacity` (no layout pass), and capped. |
| `FloatingTextDemo.cs` | Emits random numbers and shows how many labels were ever created. |
| `FloatingText.uss` | Optional look; assign it to the layer's **Style Sheet**. |

## Try it

1. Create a GameObject with a `UIDocument` and assign a PanelSettings. No UXML is needed.
2. Add `FloatingTextLayer` and `FloatingTextDemo`, assign `FloatingText.uss` and press Play.
3. The numbers rise and fade. **Labels created** settles at the peak number of simultaneous numbers; after that no
   element is created or destroyed, and nothing is allocated per number.

Notes:

- `ComponentPool<T>` is for components on GameObjects. For `VisualElement`s and plain C# objects, use
  `UnityEngine.Pool.ObjectPool<T>` as this sample does.
- Hiding with `display: none` keeps the element out of layout and rendering. Do not remove and re-add elements per
  number; that rebuilds them.
- Set position once (`left` and `top`), then animate `translate` and `opacity` only: transforms and opacity are cheap,
  layout properties are not.
- Cap the number of live elements; when the cap is reached the oldest one is recycled.
