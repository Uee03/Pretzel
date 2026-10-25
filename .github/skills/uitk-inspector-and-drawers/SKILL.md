---
name: uitk-inspector-and-drawers
description: "Build custom inspectors (CustomEditor.CreateInspectorGUI) and property drawers (PropertyDrawer.CreatePropertyGUI) with UI Toolkit, including SerializedObject binding and PropertyField. Use when customizing how a component, ScriptableObject or serialized field is shown in the Inspector."
---

# Inspectors and Property Drawers (UI Toolkit)

## Custom inspector

```csharp
[CustomEditor(typeof(MyComponent))]
public class MyComponentEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        VisualElement root = new VisualElement();
        root.AddToClassList("pretzel-my-component");
        root.Add(new PropertyField(serializedObject.FindProperty("m_Speed")));
        return root;
    }
}
```

- Return the root. Unity binds the returned tree to `serializedObject` automatically.
- To keep the default fields and add more, use `InspectorElement.FillDefaultInspector(root, serializedObject, this)`.
- Target the specific type. Never `[CustomEditor(typeof(ScriptableObject), true)]` or other broad base types.

## Property drawer

```csharp
[CustomPropertyDrawer(typeof(SerializableGuid))]
public class SerializableGuidDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        VisualElement root = new VisualElement();
        PropertyField field = new PropertyField(property.FindPropertyRelative("m_Value"), property.displayName);
        root.Add(field);
        return root;
    }
}
```

- Do not override `OnGUI`. An empty IMGUI override hides the field wherever IMGUI is used.
- Use `PropertyRelative` paths, never hardcode the parent path.

## Binding

| Where               | How                                                                        |
| ------------------- | -------------------------------------------------------------------------- |
| Inspector or drawer | Automatic for the returned root                                            |
| `EditorWindow`      | `root.Bind(new SerializedObject(target))`                                  |
| Per field           | `field.bindingPath = "m_Speed"`, then bind the parent                      |
| React to change     | `field.RegisterValueChangedCallback` or `TrackPropertyValue(property, cb)` |

- Use `SerializedObject.ApplyModifiedProperties` only when you edit properties by code. Bound fields apply themselves.
- Use `Undo` only for non-serialized changes. Bound fields record undo for you.
- Show or hide parts based on a property by tracking it and toggling `style.display`.

## Rules

- Style with `pretzel-` classes and a USS file (see `uitk-uss-style-system`). Load the USS in `CreateInspectorGUI` with `root.styleSheets.Add(...)`.
- Editor-only class in an `Editor/` folder with the Editor asmdef.
- Reuse custom controls from the package instead of rebuilding them (see `uitk-custom-controls`).
