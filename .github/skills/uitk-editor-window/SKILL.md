---
name: uitk-editor-window
description: "Create Unity Editor windows, menu items, settings pages and persisted tool state with UI Toolkit (UXML, USS, EditorWindow.CreateGUI, SettingsProvider, ScriptableSingleton). Use when building an editor tool window, Project Settings page, preferences page, or toolbar/menu entry for Pretzel."
---

# UI Toolkit Editor Window

## Folder layout (per Pretzel package)

```
Editor/
  Pretzel.<Module>.asmdef        # includePlatforms: ["Editor"]
  Layouts/<Feature>.uxml
  Styles/<Feature>.uss
  Scripts/Windows/<Feature>Window.cs
  Scripts/Settings/ | MenuItems/ | Controls/
```

Match the asmdef that already exists in the package. Namespace `Pretzel.<Module>.Windows`.

## Window template

```csharp
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pretzel.Editor.Windows
{
    public class ExampleWindow : EditorWindow
    {
        const string k_Root = "Packages/com.uee03.pretzel.editor/Editor/";
        const string k_Layout = k_Root + "Layouts/ExampleWindow.uxml";
        const string k_Style = k_Root + "Styles/ExampleWindow.uss";

        [MenuItem("Tools/Pretzel/Example")]
        static void Open()
        {
            var window = GetWindow<ExampleWindow>();
            window.titleContent = new GUIContent("Example");
            window.minSize = new Vector2(360, 240);
        }

        void CreateGUI()
        {
            var root = rootVisualElement;
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_Layout).CloneTree(root);
            root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(k_Style));

            var button = root.Q<Button>("example-window__apply-button");
            button.clicked += OnApply;
        }

        void OnApply() { }
    }
}
```

- Build the tree in `CreateGUI`, never in the constructor. It reruns after domain reload.
- Query by `name` once and keep the references. Use `Q<T>("name")` or `Q<T>(className: "...")`.
- Use `clicked +=` or `RegisterCallback`. Unsubscribe in `OnDisable` for anything tied to static or long-lived objects (`EditorApplication` events, `Selection.selectionChanged`, `Undo.undoRedoPerformed`).
- Load assets with a `Packages/<id>/...` path constant. Do not rely on `Assets/` paths.
- Use `schedule.Execute(...)` for timers. Do not poll in `EditorApplication.update`.

## Persisting state

| Need                        | Use                                                                                                                                     |
| --------------------------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| Survives domain reload only | `SessionState`                                                                                                                          |
| Per-user, all projects      | `EditorPrefs`                                                                                                                           |
| Per-user, one project       | `ScriptableSingleton<T>` with `[FilePath("UserSettings/Pretzel/X.asset", FilePathAttribute.Location.ProjectFolder)]`, then `Save(true)` |
| Shared with the team        | `ScriptableSingleton<T>` with `[FilePath("ProjectSettings/Pretzel/X.asset", ...)]`                                                      |

Never store settings in `AppData` or hidden user folders outside the project.

## Settings page

```csharp
[SettingsProvider]
static SettingsProvider Create() => new SettingsProvider("Project/Pretzel", SettingsScope.Project)
{
    label = "Pretzel",
    activateHandler = (_, root) => { /* build UI into root */ },
    keywords = new[] { "pretzel" }
};
```

## Rules

- Menu paths start with `Tools/Pretzel/`. Keep them short and cross-platform (no `explorer.exe`; use `EditorUtility.RevealInFinder`).
- Use `ListView` (with `makeItem`, `bindItem`, `fixedItemHeight`) for any list longer than a few items.
- Long tasks: show `EditorUtility.DisplayProgressBar` or a `ProgressBar` element and keep the UI responsive.
- Wrap changes to assets in `Undo.RecordObject` or use a bound `SerializedObject`.
- Keep window logic small. Move non-UI logic into plain classes so it can be reused.

Style naming and USS rules: see the `uitk-uss-style-system` skill.
