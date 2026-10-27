# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [0.2.0] - 2026-10-27

First published release. Version 0.1.0 was internal and never tagged.

### Added

- `Guard` argument checks and the `Singleton`, `PersistentSingleton` and `RegulatorSingleton` bases.
- Timers: `CountdownTimer`, `StopwatchTimer`, `Cooldown` and `IntervalTimer`, ticked by the owner, and `AwaitableDelay` for scaled or unscaled waits.
- `DisposableAction` for subscriptions that unregister on dispose.
- EditMode tests for guards, timers and disposables.
