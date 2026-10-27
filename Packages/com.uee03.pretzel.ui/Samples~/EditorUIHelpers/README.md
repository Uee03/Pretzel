# Editor UI Helpers

| File | What it shows |
|------|---------------|
| `Editor/EditorUIHelpersWindow.cs` | An `EditorWindow` built from `EditorUI`: header, help box, persisted section, bound property fields, folder picker, separator and a button row. |
| `Editor/ExportSettingsInspector.cs` | `InspectorBase`: default fields plus a field shown only when a toggle is on, using `TrackPropertyValue`. |
| `Editor/ExportSettings.cs` | The ScriptableObject being edited. |
| `Editor/Pretzel.UI.Samples.Editor.asmdef` | Editor code must reference `Pretzel.UI.Editor` from an asmdef; it is not auto-referenced. |

## Try it

Open **Window > Pretzel Samples > Editor UI Helpers**. **Inspect** selects the settings object, so the Inspector shows
the custom inspector.

Rules:

- Bind to a `SerializedObject` so Undo, multi-object editing and prefab overrides work. Use `Undo.RecordObject` for
  values you set from code, as the folder field does.
- Give a section a persistence key (`viewDataKey`) that is unique within its window or inspector.
- Elements carry `pretzel-editor__*` classes and the root gets `pretzel-editor--dark` or `--light`, so a stylesheet can
  follow the editor skin.
