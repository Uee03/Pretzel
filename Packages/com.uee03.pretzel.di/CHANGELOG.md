# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [0.2.0] - 2026-10-27

First published release. Version 0.1.0 was internal and never tagged.

### Added

- `ContainerBuilder` and `Container`: transient, scoped and singleton lifetimes, constructor and `[Inject]` member injection, build-time validation, child scopes and disposal.
- Entry points (`IStartable`, `ITickable`, `ILateTickable`, `IFixedTickable`), `LifetimeScope`, `ScriptableInstaller` and injected prefab instantiation.
- EditMode tests.
