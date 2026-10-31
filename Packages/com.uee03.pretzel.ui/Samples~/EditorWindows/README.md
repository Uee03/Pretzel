# Editor Windows

UI Toolkit `EditorWindow` samples, one folder per topic. Every window opens from **Pretzel > Samples > Editor Windows**.

| Folder | Menu item | What it shows |
|--------|-----------|---------------|
| `Basics` | Basics | Code workflow: `CreateGUI`, toolbar, fields, buttons, help boxes and three ways to persist state (`SessionState`, `EditorPrefs`, a `ScriptableSingleton`). |
| `GroupBoxes` | Group Boxes | UXML + USS workflow: `GroupBox`, `Foldout`, `ScrollView` and styled sections. |
| `Lists` | Lists | A virtualized `ListView` over a plain C# list: `makeItem`/`bindItem`/`unbindItem`/`destroyItem`, add and remove, reordering, multi-selection, and a counter that shows how few rows exist for many items. |
| `Tabs` | Tabs | `TabView` and `Tab` in UXML with the content of each tab built in code. |
| `Tables` | Tables | A `MultiColumnListView` over project assets: sortable and stretchable columns, a data model class, filtering with a `ToolbarSearchField` and a button column. |
| `Dashboard` | Dashboard | A combined window: UXML layout, stat cards, a `ProgressBar` driven by a scheduled scan, and buttons that select, ping and reveal real assets. |
| `Shared` | - | `SampleAssets` finds the UXML and USS next to the scripts by name (imported samples land in `Assets/Samples/...`, so fixed paths would break) and `SampleWindows.uss` holds common spacing. |

## Try it

1. Import the sample from the Package Manager (**Pretzel UI > Samples > Editor Windows**).
2. Open any window from the **Pretzel > Samples > Editor Windows** menu and read its source next to it.

What to take from it:

- Pick one workflow per window: build in C# (`Basics`, `Lists`, `Tables`) when the layout depends on data, and UXML + USS
  (`GroupBoxes`, `Tabs`, `Dashboard`) when designers will edit it.
- Create cells and rows once and rebind them; never rebuild a row on every bind.
- Register callbacks once, keep the bound item in a field and unregister on detach.
- Persist small state in `SessionState` (one Editor session), `EditorPrefs` (per user) or a `ScriptableSingleton`
  (per project).

The sample compiles into its own Editor-only assembly, `Pretzel.UI.Samples.EditorWindows`.
