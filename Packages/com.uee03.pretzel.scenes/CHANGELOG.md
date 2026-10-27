# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [0.2.0] - 2026-10-27

First published release. Version 0.1.0 was internal and never tagged.

### Added

- `SceneLoader`: gated single and additive loads with progress, minimum duration, unload of unused assets and cancellation.
- A setup phase behind the loading screen: `SceneLoadOptions.SetupAsync` with `SceneSetupContext`, `SceneProgressWeight`, `SettleFrames` and `LoadingStatus`; one progress value for the whole load that never goes backwards; `SubProgress`; `Scene.FindComponent<T>()`.
- `ISceneTransition`, `ISceneStatusDisplay` and the fading `UIDocumentSceneTransition` loading screen with a percentage and a status label.
- Level Loading sample: additive levels whose pooled enemies spawn from placed markers behind the loading screen.
- EditMode tests.
