---
name: unity-editor-tools
description: "Build Unity EditorWindows, custom inspectors, PropertyDrawers, menu tools, asset validation, and UI Toolkit Editor extensions. Use for serialized editing, Undo, prefab overrides, multi-object editing, or reusable package Editor integrations."
---

# Unity Editor tools

## Workflow

1. Inspect existing Editor utilities and the task's authoring workflow. Reuse compatible helpers
   such as installed LazyJedi drawers/windows; do not require those game addons in a generic package.
2. Keep `UnityEditor` code in an Editor-only asmdef with explicit runtime references. Confirm
   the minimum supported Editor APIs and UI Toolkit vs IMGUI needs before implementing controls.
3. For inspectors/drawers, use SerializedObject/SerializedProperty update/apply lifecycles and
   support multi-object selection, mixed values, and prefab overrides. Avoid direct field writes
   when serialized editing already provides the right behavior.
4. For direct object edits, use appropriate Undo APIs before mutation and prefab-instance
   modification recording where needed. Dirty marking is not a substitute for Undo or saving.
5. For hierarchy/asset creation, use supported Undo/AssetDatabase APIs and preserve `.meta` GUIDs.
   Require confirmation for destructive or bulk edits; provide preview/dry-run when warranted.
6. Keep `OnValidate` safe and local; it can run during import or outside normal gameplay timing.
   Schedule explicit Editor operations rather than starting scene creation/async workflows there.
7. Windows own callbacks, selections, scheduled work, and subscription disposal across reloads
   and closure. Keep expensive queries out of repaint/OnGUI loops unless cached and invalidated.
8. Verify multi-selection, Undo/Redo, prefab override persistence, reload, window reopening, asset
   save/reload, and player compilation with Editor assemblies excluded.

## DO NOT

- Reference Editor assemblies from Runtime, mutate target fields without recording Undo, or
  suppress serialization/prefab requirements just because a single-object demo works.
- Rename/delete assets outside approved scope, discard `.meta`, or rewrite scene/prefab YAML.
- Assume EditorWindow code proves graph APIs are public; verify actual Shader/VFX Graph APIs first.
- Automatically add an Editor automation server. Optional external tools require explicit approval.

## Sources

- [EditorWindow](https://docs.unity3d.com/ScriptReference/EditorWindow.html)
- [SerializedObject](https://docs.unity3d.com/ScriptReference/SerializedObject.html)
- [Undo](https://docs.unity3d.com/ScriptReference/Undo.html)
- [PrefabUtility](https://docs.unity3d.com/ScriptReference/PrefabUtility.html)
- UI Toolkit Editor UI: [uitk-editor-window](../uitk-editor-window/SKILL.md), [uitk-inspector-and-drawers](../uitk-inspector-and-drawers/SKILL.md)
- [Unity-Skills advisory/tool separation](https://github.com/Besty0728/Unity-Skills)
