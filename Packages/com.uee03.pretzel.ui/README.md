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

## In-game helpers

- `UIScreenStack`: navigation between screens. `Push` fades a screen in and focuses its first control; `Push(screen, modal: true)`
  keeps the screen below visible for dialogs and pause overlays; `Pop`, `Replace`, `PopToRoot`, `Clear`. Hook `Pop` to
  `NavigationCancelEvent` for Escape and the gamepad back button.
- `ToastService`: short notifications in a container, styled with `pretzel-toast` and `--info`, `--success`, `--warning`,
  `--error` modifiers.
- `SafeAreaElement` (UXML `<Pretzel.UI.SafeAreaElement>`): pads its content to `Screen.safeArea` and updates on resize.
  `SafeAreaInsets.Compute` is the pure calculation.
- `VisualElementExtensions`: `QRequired<T>` (throws with a clear message when the UXML lacks an element), `OnClick`,
  `OnValueChanged` and `RegisterDisposable` returning `IDisposable`, `FocusFirst`, `SetDisplayed`, `SetVisible`,
  `SetPickingModeRecursive`.

```csharp
protected override void OnBind(VisualElement root)
{
    _bindings.Add(root.QRequired<Button>("options").OnClick(() => _screens.Push(_options)));
    _bindings.Add(root.RegisterDisposable<NavigationCancelEvent>(_ => _screens.Pop()));
}

protected override void OnUnbind(VisualElement root) => _bindings.ForEach(b => b.Dispose());
```

## World and world-space UI

- `UIFollowTarget` keeps an element of a screen-space document over a world object (health bars, name tags, markers),
  moving it with `translate` and hiding it behind the camera. `WorldToPanel` has the conversion helpers.
- Unity 6.2 and newer render UI Toolkit in world space (PanelSettings > Render Mode > World Space). Put the `UIDocument`
  on the object, use a separate PanelSettings asset for world-space documents, and add `UIBillboard` to face the
  camera (match camera, face camera or upright), optionally at a constant screen size. `WorldSpaceUIExtensions.IsWorldSpace`
  is compiled on 6.2 or newer only.

## Themes

Create a runtime theme with **Assets > Create > Pretzel > UI Toolkit Theme** (Pretzel Templates) and assign it to
PanelSettings > Theme Style Sheet. It imports Unity's default runtime theme and overrides it through variables.

## Editor helpers

The `Pretzel.UI.Editor` assembly (reference it from your editor asmdef) has building blocks for inspectors, drawers and
windows:

- `EditorUI`: `Header`, `Section` (with persisted open state), `Help`, `Separator`, `Row`, `Button`, `ButtonRow`,
  `PropertyFields` (bound fields for every visible property, like the default inspector), `FolderField` and `FileField`
  with browse buttons and project-relative paths, `LoadStyleSheet`, `LoadLayout`, `ApplySkinClass`.
- `InspectorBase`: a `CreateInspectorGUI` base with an optional stylesheet, the skin class and `AddDefaultProperties`.

## Samples

- `Samples~/UIUsage`: a controller that builds a panel and fades it with the extension methods.
- `Samples~/GameUIHelpers`: a menu with an options screen, a modal dialog, toasts, a safe-area root and a name tag.
- `Samples~/EditorUIHelpers`: an editor window and a custom inspector built with `EditorUI` and `InspectorBase`.
