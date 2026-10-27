# Setup Extensions

Adds content to **Pretzel > Project Setup** from your own code. The window finds every
implementation with `TypeCache`, so no registration is needed.

| File | What it shows |
|------|---------------|
| `FolderChecklistTab.cs` | An `IPretzelTab` named *Folder Checklist*: reads and edits `PretzelTabContext.Project`, saves with `MarkProjectDirty()`, rebinds on `SettingsReset`, and refreshes in `OnSelected()`. |
| `ModuleSourceSection.cs` | An `IPretzelMainSection` shown at the top of the Main tab, which returns `null` to hide itself once a module source is known (Settings > Repository URL, or a git install of Pretzel Setup). |

## Try it

1. Import the sample and let Unity compile.
2. Open **Pretzel > Project Setup**. A *Folder Checklist* tab appears before *Settings*.
3. Pick a preset and press **Create missing folders**.

## Use it in your own package

- Put the classes in an Editor-only assembly that references `Pretzel.Setup.Editor`, like the asmdef here.
- Give the class a public parameterless constructor. `Order` sorts tabs (Main 0, Addons 10,
  Addons Extra 20, Settings 100) and Main tab sections (lower first).
- Use the `pretzel-section*` USS classes to match the window's style.

Delete this folder to remove the tab and the section.
