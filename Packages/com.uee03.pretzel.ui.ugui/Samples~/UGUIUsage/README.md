# UGUI Usage

`UIFader`, `OnMouseHoverFade` and the Button and Graphic extensions.

## Set up the scene

1. **GameObject > UI > Panel**. Add a **Canvas Group**, a **UI Fader** and `UGUIUsageExample`.
2. Add three buttons outside the panel (so they stay clickable when it fades) and assign them to **Show Button**,
   **Hide Button** and **Snap Button**.
3. Optional: add a **TextMeshPro - Input Field** inside the panel and assign it to **Name Input**. Its placeholder
   shows the panel state through `SetPlaceHolderText`.
4. Hover tooltip: add a child **Text - TextMeshPro** with its own **Canvas Group** and **UI Fader** (set
   **Fade On Start** to *Fade Out Now*), then add **On Mouse Hover Fade** to the object that should react to the
   pointer and assign the tooltip's UI Fader.
5. Press Play.

| Feature | Where |
|---------|-------|
| `FadeIn`, `FadeOut`, `FadeInNow`, `FadeOutNow`, `Alpha`, `FadeInDuration`, `FadeOutDuration` | `UGUIUsageExample` |
| `AddListener`, `RemoveListener`, `RemoveAllListeners`, `SetPlaceHolderText` | `UGUIUsageExample` |
| Pointer enter and exit (UI) or OnMouseEnter/Exit (colliders) | `OnMouseHoverFade` |

A UI Fader can fade a list of **Canvas Renderers** instead of a Canvas Group: untick **Use Canvas Group** and
fill the list. Untick **Use Ease Type** to fade with the animation curves.
