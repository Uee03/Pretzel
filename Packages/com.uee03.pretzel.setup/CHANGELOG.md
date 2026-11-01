# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- `PretzelSetup` (`CreateFolders`, `GenerateComposition`, `ApplyComposition`) and `CompositionResult`: the window's folder and VContainer features as a public API.
- `PretzelSetupBatch.Folders` and `PretzelSetupBatch.Composition`: `-executeMethod` entry points that bootstrap a project from the command line in one run, with `-pretzelNamespace`, `-pretzelScope`, `-pretzelServices` and `-pretzelStarter` overrides. The composition run quits by itself (exit code 1 on failure, including generated code that does not compile).
- The starter `IGameService` has a `ProgressLoaded` event, raised when saved progress finishes loading, so a screen no longer has to poll `IsLoaded`.
- The generated composition and the Setup VContainer tab now look for the Pretzel VContainer module (`com.uee03.pretzel.vcontainer`), which replaces the VContainer part of Integrations. The assembly name is unchanged, so generated code keeps compiling.
- Sample **No Container**: Click Rush, a complete small game with no DI container. A `GameServices` ScriptableObject filled by a `BeforeSceneLoad` bootstrap, Events, Scenes, Data and Preferences, UI Toolkit screens, a scene builder, 18 EditMode tests and a PlayMode test that plays menu, round and result through the UI.
- Documentation: `Documentation~/WithoutAContainer.md`, a short guide to sharing services with ScriptableObjects and a bootstrap instead of a DI container, linked from the VContainer tab.
- The Addons tab shows the VContainer chip on the Pretzel VContainer module and lists it before Integrations.

### Changed

- The generated VContainer registrations for the API client, pool registry, audio service and preferences service now also register their interfaces (`IApiClient`, `IPoolRegistry`, `IAudioService`, `IPreferencesService`) next to the concrete type.
- The scene templates message no longer mentions the removed Templates package.
- The Game preset also creates `_Project/UI/Layouts` and `_Project/UI/Styles`, and every preset creates `_Scripts/Tests`. Saved presets are not rewritten; use the Settings tab reset to pick up the new defaults.

### Fixed

- `pretzel-settings.json` no longer stores the computed `ActivePreset`; it is derived from `folderPresets` and `activeFolderPreset`.
- The scan for a project `ISaveService`/`SaveService` no longer enumerates the AppDomain (Unity analyzer UAC0005); it asks each compiled assembly by name.
- Starter services no longer skip `ISaveService`/`SaveService` because a file with that name exists somewhere under `Assets`. The project's own pair (for example the Pretzel Data examples) is reused only when both types are compiled, `SaveService` implements `ISaveService` with `LoadOrCreateAsync` and `Save`, and the code folder's assembly can see them; otherwise the starter copies are written in the composition namespace.

## [0.2.0] - 2026-10-27

First published release. Version 0.1.0 was internal and never tagged.

### Added

- The Project Setup window: project info, folder presets, scene templates, first-run starter kit and the Addons and Addons Extra tabs.
- The VContainer tab: installs VContainer, generates the composition root code and creates the root scope, installer and VContainerSettings assets, and toggles VContainer diagnostics. It links to the Pretzel guide and the official VContainer documentation.
- VContainer tab starter services: ready-to-use `ISaveService`/`SaveService` and `IGameService`/`GameService`, and a `GameServicesExample`, generated from templates in `Editor/Templates/Composition`.
- The module catalog is generated from the module manifests (`.github/scripts/Update-ModuleCatalog.ps1`) into `Editor/Catalog/modules.json`, so Addons tab dependencies can no longer drift from `package.json`.
- EditMode tests that check the catalog against the installed manifests.
