# UI Toolkit Usage

Two screens built with `UIControllerBase`, plus an Editor window for the Editor-only factories.

| File | What it shows |
|------|---------------|
| `UIUsageController.cs` + `UIUsage.uxml` | Fading with the extension methods and `FadeType`, `Cancel` and `IsFading`, a separate `UIToolkitFader` with `HideOnFadeOut`, `ToEasingMode`, `Show`/`Hide`/`ShowDelayed`, `SwitchTo`, and every runtime `UIToolkitUtility` factory |
| `UIUsageOverlayController.cs` + `UIUsageOverlay.uxml` | A second screen faded in with `UIDocumentFader` |
| `UIUsage.uss` | Styles shared by both documents |
| `Editor/UIToolkitUtilityWindow.cs` | **Tools > Pretzel Samples > UI Toolkit Utility Window**: toolbar items, Tag, Layer, Mask, EnumFlags, Object and Property fields |

## Set up the scene

1. **GameObject > UI Toolkit > UI Document** and name it `Main`. Set **Source Asset** to `UIUsage.uxml`.
   Add `UIUsageController` and `UIHoverFade`. On `UIHoverFade` set **Hover Element Name** to `card`,
   **Fade Element Name** to `hover-hint`, and tick **Start Hidden**.
2. Create a second UI Document named `Overlay` with `UIUsageOverlay.uxml`, the same Panel Settings and a higher
   **Sort Order**. Add `UIUsageOverlayController` (it adds `UIDocumentFader`). Leave **Fade On Start** off.
3. Assign `Overlay` to **Overlay** on `UIUsageController`, and `Main` to **Main** on `UIUsageOverlayController`.
4. Press Play.

`ActivateGameObject` and `DeactivateGameObject` are meant for UnityEvents, such as a UGUI button's On Click list.
