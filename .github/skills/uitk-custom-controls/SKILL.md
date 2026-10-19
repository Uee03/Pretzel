---
name: uitk-custom-controls
description: "Create reusable UI Toolkit custom controls with [UxmlElement] and [UxmlAttribute] in Unity 6, including lifecycle, events, BaseField value controls and USS classes. Use when building a reusable element such as a toggle, health bar, tab view, path picker or any new VisualElement."
---

# Custom Controls (Unity 6)

## Template

```csharp
using UnityEngine.UIElements;

namespace Pretzel.Editor.Controls
{
    [UxmlElement]
    public partial class PathField : BaseField<string>
    {
        public static readonly string ussClassName = "pretzel-path-field";

        [UxmlAttribute("browse-label")]
        public string BrowseLabel { get; set; } = "Browse";

        public PathField() : this(null) { }

        public PathField(string label) : base(label, new VisualElement())
        {
            AddToClassList(ussClassName);
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);
        }

        void OnAttach(AttachToPanelEvent evt) { }
        void OnDetach(DetachFromPanelEvent evt) { }
    }
}
```

## Rules

- Use `[UxmlElement]` on a `partial` class and `[UxmlAttribute]` on properties. Do not use the obsolete `UxmlFactory` and `UxmlTraits`.
- Always provide a parameterless constructor so UXML and UI Builder can create it.
- Pick the closest base: `VisualElement` for containers, `BaseField<T>` for value controls, `Button` or `Label` for specialized versions.
- There is no `Awake`, `OnEnable` or `OnDestroy`. Build children in the constructor. Use `AttachToPanelEvent` for work that needs a panel and `DetachFromPanelEvent` to unsubscribe from anything outside the control.
- Update visuals from `SetValueWithoutNotify` and call `base.SetValueWithoutNotify` first. This avoids event loops.
- Raise changes through `value = ...` so `ChangeEvent<T>` fires. Call `evt.StopPropagation()` for events the control fully handles.
- Support keyboard and navigation events (`KeyDownEvent`, `NavigationSubmitEvent`) for interactive controls, not only mouse clicks.
- Add BEM classes to child parts: `pretzel-path-field__input`, `pretzel-path-field__browse-button`.
- Put control styles in `Styles/Components/<Name>.uss`. Expose colors and sizes as USS variables, not C# properties.
- Public `ussClassName` constants mirror Unity's own controls so users can restyle them.

## Using in UXML

```xml
<engine:UXML xmlns:engine="UnityEngine.UIElements" xmlns:pretzel="Pretzel.Editor.Controls">
  <pretzel:PathField label="Output" browse-label="Pick" />
</engine:UXML>
```

The UXML namespace is the control's C# namespace. Confirm it in UI Builder's Library panel.

## Placement

- Editor-only controls: the package's `Editor/Scripts/Controls/`.
- Controls usable at runtime: `pretzel.ui` `Runtime/Scripts/CustomElements/`. Only reference `UnityEditor` inside `#if UNITY_EDITOR` or an Editor asmdef.

## Pooling

Reset a pooled control fully before reuse: clear callbacks, values and USS state classes. Do not pool elements that register events without a matching unregister.
