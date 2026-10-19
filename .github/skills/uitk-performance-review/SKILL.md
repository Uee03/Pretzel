---
name: uitk-performance-review
description: "Review UI Toolkit code and assets for performance and memory problems: element counts, hiding methods, animations, batching, masking, bindings, pooling and leaks. Use before finishing any UI Toolkit tool, or when asked to optimize, audit or profile a UI Toolkit UI."
---

# UI Toolkit Performance Review

Run through this list before finishing a tool. Report only the items that apply.

## Elements and hierarchy

- Lists of more than a handful of items use `ListView` (virtualized). Large trees use `TreeView`.
- Hierarchies are shallow. No wrapper elements that only add nesting.
- Elements pooled by hand are fully reset (callbacks, values, classes) before reuse.

## Showing and hiding

| Use                                 | When                                                      |
| ----------------------------------- | --------------------------------------------------------- |
| `style.display = DisplayStyle.None` | Toggled often. No render or layout cost.                  |
| `RemoveFromHierarchy()`             | Rarely shown (dialogs). Frees memory but costs a rebuild. |
| `visibility: hidden`                | Layout must be kept.                                      |
| `opacity: 0` or moving off-screen   | Only for transitions. Still costs GPU and layout.         |

## Styles and animation

- Animate `translate`, `scale`, `rotate`, `opacity`. Not `width`, `height`, `top`, `left`.
- Set `usageHints` (`DynamicTransform`, `GroupTransform`) on elements that animate.
- No class swaps on big subtrees every frame. Prefer inline property changes during animation.
- Selectors are specific. No `*` or deep descendant chains over large trees.

## Rendering (runtime UI)

- Keep textures per batch within the 8-texture limit. Use a Sprite Atlas for static art and dynamic atlas settings in `PanelSettings` for runtime art.
- Prefer rectangular masks (`overflow: hidden`) over rounded-corner masks. Keep nesting flat.
- Avoid stacked transparent elements. Hide obscured ones with `display: none`.
- Raise Vertex Budget in `PanelSettings` only if the Frame Debugger shows split draw calls.

## Events, memory and leaks

- Every `RegisterCallback`, `+=` on a long-lived object, or `EditorApplication` subscription has a matching removal (`OnDisable`, `DetachFromPanelEvent`).
- `schedule` items are stopped (`Pause()`) when no longer needed.
- No per-frame allocations in callbacks (LINQ, string concatenation, lambdas capturing in loops).
- Editor tools do not poll in `EditorApplication.update`.
- Referenced assets (fonts, textures) are loaded only when needed.

## Bindings

- See the Performance section of `uitk-data-binding`: fewer bindings, change tracking, `[CreateProperty]`.

## Tools

Use the UI Toolkit Debugger, Frame Debugger and Profiler to confirm. `PanelSettings.SetPanelChangeReceiver` logs which elements change (editor and development builds only).
