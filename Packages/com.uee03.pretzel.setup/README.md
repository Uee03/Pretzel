# Pretzel Setup

The Project Setup window. It needs no other Pretzel module, only `com.unity.nuget.newtonsoft-json`, so it can be the first and only thing you install, then used to install the rest. Editor only.

Open it from `Pretzel > Project Setup` (Shift + Alt + P). It opens once by itself the first time.

## Tabs

- **Main**: company, product, version, icon and cursor, a folder structure preset (Game, Tool, System or Custom) with a Create Folders button, and a picker for the 2D and 3D scene templates in this package. On first run it also offers a starter kit: a preset that creates its folders and installs the recommended addons in one step.
- **Addons**: one card per Pretzel module, with its installed version, dependencies and Install, Uninstall or Update. Modules already in the project show as Local. Embedded modules (inside this project's `Packages` folder) show a disabled Uninstall, since removing them would delete the folder. The integrations module shows a chip for each third-party library it adapts (LitMotion, UniTask, R3) and which of them are installed.
- **Addons Extra**: a curated catalog of third-party packages with search, a pinned version, installed state and notes.
- **Settings**: folder presets, the settings folder, catalog sources, per-user paths and an About section, with a shortcut to the Data Format window when `pretzel.data` is installed. Each section has a reset.

Changes are saved a moment after you stop typing and when the window closes.

Settings are JSON files:

| File | Scope |
|---|---|
| `ProjectSettings/Pretzel/pretzel-settings.json` | Project wide. Commit it |
| `UserSettings/Pretzel/pretzel-user.json` | Per user. Keep it out of version control |

A missing or corrupt file falls back to defaults. A corrupt file is replaced only when something is saved, and the old one stays as `.bak`.

## Addons

| Entry | Source |
|---|---|
| NuGetForUnity 4.5.0, UniTask 2.5.11, LitMotion v2.0.2, R3 1.3.1, ZLinq 1.5.6, VContainer 1.19.0 | Git URL with a pinned tag |
| Newtonsoft.Json 3.2.1 | Unity registry |

Installs and removals use the Package Manager API in one call. Uninstall asks for confirmation and is blocked while another installed package depends on the package. R3 and ZLinq also need their NuGet package: the card installs NuGetForUnity first, then opens the NuGet window, and Install stays disabled until `packages.config` lists the package.

- Pins can be changed per project on the Addons Extra tab.
- Catalog files (JSON, see `CatalogFile`) listed under Settings > Catalog sources add entries or replace built-in ones by id.
- Pretzel modules install from the git repository and tag that this package was installed from. The Settings tab (`moduleSourceUrl`, `moduleSourceVersion`) overrides that. When Setup is embedded or local and no URL is set, Install stays disabled.

### Module catalog

The Addons tab reads `Editor/Catalog/modules.json`. It is generated from every module's `package.json` (display
name, description, Pretzel dependencies, required packages) and from the integration asmdefs (optional libraries) by
`.github/scripts/Update-ModuleCatalog.ps1`. Run the script after changing a module manifest, and use `-Check` before
a release; `ModuleCatalogTests` also compares the catalog with the installed manifests. Do not edit the JSON by hand.

## Recommended folder structure

All scripts live in `Assets/_Scripts`, split into `Editor` and `Runtime`. Everything else lives in `Assets/_Project`. The leading underscore sorts both to the top of the Project window. The presets create these folders and can be edited on the Main tab.

```
Assets/
  _Scripts/
    Editor/
    Runtime/
  _Project/
    Animations/   Art/{Materials,Models,Shaders,Sprites,Textures}/   Audio/{BGM,SFX}/
    Prefabs/   Scenes/   ScriptableObjects/   Settings/   UI/{Fonts,Icons}/
```

| Preset | Use for | Under `_Project` |
|---|---|---|
| Game | A playable game | Animations, Art, Audio, Prefabs, Scenes, ScriptableObjects, Settings, UI |
| Tool | An editor or runtime tool | Documentation, Prefabs, Samples, Scenes, ScriptableObjects, Settings, UI (Icons, Layouts, Styles) |
| System | A reusable system such as saving | Documentation, Prefabs, Samples, Scenes, ScriptableObjects, Settings |
| Custom | Your own layout | Prefabs, Scenes, ScriptableObjects |

Third-party assets stay in their own folders (`Plugins`, `Addons`, package folders) and are never mixed into `_Project`. Old `_Project/Scripts/...` entries in saved presets are rewritten to `_Scripts/...` when settings load.

## Scene templates

`Pretzel 2D` and `Pretzel 3D` appear in `File > New Scene` and on the Main tab. Both group their objects under `[CAMERA]`, `[LIGHTING]`, `[ENVIRONMENT]`, `[MANAGERS]` and `[UI]`.

- 2D: orthographic camera on a solid background.
- 3D: perspective camera, a directional light and a ground plane.

## Menus

- `Pretzel > Create > Folders`: creates the folders of the active preset.
- `Pretzel > Open`: Application Data Path, Persistent Data Path, Resources Folder, Temporary Folder.

## Extending the window

Add a tab by implementing `IPretzelTab`, or a card on the Main tab with `IPretzelMainSection`. Both are found with `TypeCache`, and live in `Pretzel.Setup.Tabs`.
