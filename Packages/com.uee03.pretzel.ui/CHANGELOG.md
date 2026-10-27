# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [0.2.0] - 2026-10-27

First published release. Version 0.1.0 was internal and never tagged.

### Added

- Element factories, USS-transition fades, `UIDocumentFader`, `UIHoverFade` and `UIControllerBase`.
- In-game helpers: `UIScreenStack` navigation with modal screens, `ToastService`, `SafeAreaElement`, and `VisualElementExtensions` (`QRequired`, disposable callbacks, `FocusFirst`, display helpers).
- World helpers: `UIFollowTarget` and `WorldToPanel` for screen-space markers over world objects, `UIBillboard`, and `WorldSpaceUIExtensions` for world-space panels on Unity 6.2 or newer.
- Editor helpers (`Pretzel.UI.Editor`): `EditorUI` building blocks and `InspectorBase`.
- Samples: Game UI Helpers and Editor UI Helpers.
- EditMode tests for the screen stack, safe area and element helpers.
