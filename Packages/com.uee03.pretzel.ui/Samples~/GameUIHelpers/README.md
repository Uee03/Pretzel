# Game UI Helpers

| File | What it shows |
|------|---------------|
| `GameUIHelpers.uxml` | A `SafeAreaElement` root with a menu, an options screen, a modal dialog and a toast container. |
| `GameUIHelpersController.cs` | `UIScreenStack` (push, modal push, pop, cancel to go back), `ToastService`, `QRequired` and disposable callbacks released in `OnUnbind`. |
| `NameTagExample.cs` | A name tag that follows a 3D object with `UIFollowTarget`. |

## Try it

1. Create a GameObject with a `UIDocument`, assign a PanelSettings and `GameUIHelpers.uxml`, and add
   `GameUIHelpersController`.
2. Optional: create a theme with **Assets > Create > Pretzel > UI Toolkit Theme** (from Pretzel Templates) and assign
   it to the PanelSettings. The sample uses its `screen`, `panel` and `pretzel-toast` classes.
3. Press Play. Options pushes a screen, Quit opens a modal dialog, and the cancel action (Escape or the gamepad East
   button) goes back.
4. Add `NameTagExample` to a 3D object, assign the same document, and move the camera.

World-space UI: Unity 6.2 and newer can render a UIDocument in world space (PanelSettings > Render Mode > World Space).
Use one PanelSettings asset per render mode, put the document on the object it labels, and add `UIBillboard` so it
faces the camera. `WorldSpaceUIExtensions.IsWorldSpace` checks a document at runtime.
