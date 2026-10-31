# Custom Inspectors

Custom editors built with `CreateInspectorGUI`. Every topic has a runtime target type (in `Runtime/`) and its editor (in
`Editor/`), plus a menu item under **Pretzel > Samples > Custom Inspectors > Create** that makes an object to inspect.

| Topic | Target | What it shows |
|-------|--------|---------------|
| `DefaultPlusExtras` | `HealthStats` | Keeps the **default inspector layout** with `InspectorElement.FillDefaultInspector` and adds a live summary, a help box and buttons (Heal, Damage, Kill) below it. Buttons edit every selected object with Undo. |
| `FullyCustom` | `WeaponConfig` | A completely custom layout: `PropertyField`s found by `bindingPath`, foldout sections and groups that show or hide by weapon kind. Styled with USS. |
| `Components` | `SpawnArea` | A component editor whose buttons change the scene: Undo, prefab-instance overrides recorded, buttons disabled on prefab assets. |
| `ScriptableObjects` | `QuestDefinition` | A ScriptableObject inspector on `InspectorBase`: live validation in help boxes, a button, and **Assets > Create > Pretzel Samples > Quest Definition**. |
| `MultiObject` | `TeamMember` | `[CanEditMultipleObjects]`: mixed values, a summary over all targets and buttons that write through each object's own `SerializedObject`. |

## Try it

1. Import the sample (**Pretzel UI > Samples > Custom Inspectors**).
2. Use **Pretzel > Samples > Custom Inspectors > Create > ...** and select the new object, or add a component from
   **Add Component > Pretzel Samples**.
3. Try the buttons, then Undo (Ctrl+Z). Select several `TeamMember` objects to see multi-object editing.

Rules shown here:

- Return the tree from `CreateInspectorGUI`; Unity binds it to the `serializedObject`.
- Edit through the `SerializedObject` where you can. When you must change objects directly, call `Undo.RecordObjects`
  first and `PrefabUtility.RecordPrefabInstancePropertyModifications` after.
- `TrackSerializedObjectValue` refreshes derived UI after edits, Undo and the buttons.

Assemblies: `Pretzel.UI.Samples.CustomInspectors.Runtime` and `Pretzel.UI.Samples.CustomInspectors.Editor`.
