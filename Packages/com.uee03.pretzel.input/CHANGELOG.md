# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- `InputContextStack`, `InputBlockers` and `InputRouter`: one enabled action map per context, and input blocked for named reasons.
- `InputBuffer`: press buffering for jumps and similar.
- `InputDeviceTracker` and `InputDeviceKind`: the device kind used last, from the actions that fire.
- `RebindSession`, `RebindResult`: a safe interactive rebind.
- `BindingPersistence`, `IBindingStore` and `PlayerPrefsBindingStore`: load, save and reset rebinds.
- Optional adapters, compiled only when their module is installed: `PreferencesBindingStore` (Pretzel Preferences) and `SceneLoadInputGate` (Pretzel Scenes).
- Sample **Input Example**: contexts, pause, buffered jump, device readout, rebinding and an input block.
- EditMode tests that drive the Input System test fixture with simulated devices.
