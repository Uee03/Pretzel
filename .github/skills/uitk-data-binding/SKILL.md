---
name: uitk-data-binding
description: "Bind data to UI Toolkit in Unity 6: SerializedObject binding for editor tools and runtime data binding (CreateProperty, dataSource, dataSourcePath, DataBinding, binding modes, converters, change tracking). Use when connecting UI elements to ScriptableObjects, settings objects or view models."
---

# Data Binding in UI Toolkit

## Choose the mechanism

| Situation                                                                            | Use                                                                                 |
| ------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------- |
| Editing serialized fields of a `UnityEngine.Object` in an inspector or editor window | SerializedObject binding (`bindingPath`, `Bind`). See `uitk-inspector-and-drawers`. |
| Showing computed or non-serialized values, lists, or runtime UI                      | Runtime data binding (`dataSource`, `DataBinding`)                                  |

## Preparing the data source

```csharp
using Unity.Properties;

public class ToolState : ScriptableObject
{
    [SerializeField, DontCreateProperty] string m_Name;

    [CreateProperty]
    public string Name
    {
        get => m_Name;
        set => m_Name = value;
    }
}
```

- `[CreateProperty]` on each bindable property avoids runtime reflection. Use `[DontCreateProperty]` on the serialized backing field.
- Any C# object can be a data source. Structs reduce allocations but watch boxing.

## Binding in C#

```csharp
root.dataSource = state;
label.SetBinding("text", new DataBinding
{
    dataSourcePath = new PropertyPath(nameof(ToolState.Name)),
    bindingMode = BindingMode.ToTarget
});
```

- Children inherit the nearest parent's `dataSource`. Set it once on the root and override only where needed.
- Modes: `TwoWay` (default, inputs), `ToTarget` (read-only displays, the usual choice), `ToSource`, `ToTargetOnce` (static values).
- UXML equivalent: `<Bindings><engine:DataBinding property="text" data-source-path="Name" binding-mode="ToTarget" /></Bindings>`.
- A fixed `data-source` in UXML cannot change at runtime. For swappable data, declare `data-source-type` in UXML and assign `element.dataSource` in C# (an unresolved binding).
- Do not define bindings for the same property in both UXML and C#.

## Lists

Bind `ListView.itemsSource` to a list property with `SetBinding("itemsSource", new DataBinding { dataSourcePath = new PropertyPath("Items") })`. Use an item template with unresolved bindings.

## Converters

Register a `ConverterGroup` when a binding needs a different type (for example `float` to `StyleColor`). Register in both `[InitializeOnLoadMethod]` (editor, UI Builder) and `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` (player) when the converter is used at runtime. Keep converters small and allocation-free.

## Performance

- By default bindings update every frame. Reduce them: bind only what changes, use flat data, cache computed values.
- Implement `INotifyBindablePropertyChanged` for property-level updates and `IDataSourceViewHashProvider` (return a version counter) for whole-object change detection.
- Use `[GeneratePropertyBag]` on hot types to avoid reflection at first use.
- For rarely changing data, assign values directly or call `MarkDirty` instead of binding.

Check `Unity.Properties` and `UnityEngine.UIElements` API names against the Unity 6.6 reference before using less common members.
