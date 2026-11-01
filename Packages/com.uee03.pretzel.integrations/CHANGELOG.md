# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- `Pretzel.Integrations.R3.Events`: R3 streams over `IEventBus` and the ScriptableObject event channels (`AsObservable`), and `PublishTo`/`RaiseOn` to feed streams back into events. Compiles only when R3 and `pretzel.events` are both installed, and adds no dependency to this package.
- R3Events sample: the bus as a stream with a running score, a combo that ends after a pause and a stream that raises a channel.

### Changed

- **Breaking:** the VContainer pieces (`PretzelLifetimeScope`, `ScriptableInstaller`, `MonoInstaller`), their tests, the guide and the VContainer Example moved to the new `com.uee03.pretzel.vcontainer` package, which needs no other Pretzel module. Projects that use them add that package. The assembly (`Pretzel.Integrations.VContainer`), the namespace and the script GUIDs are unchanged, so existing code, prefabs and scenes keep working once it is installed.
- The UniTask and R3 `ApiClient` extensions now extend `IApiClient`, so they work on fakes.

## [0.2.0] - 2026-10-27

First published release. Version 0.1.0 was internal and never tagged.

### Added

- LitMotion ease mapping, UniTask `ApiClient` calls and R3 observables for UI Toolkit and `ApiClient`, each compiled only when its library is installed.
- VContainer composition root: `PretzelLifetimeScope`, `ScriptableInstaller`, `MonoInstaller`, PlayMode tests, and a guide in `Documentation~/VContainer.md` (scopes, child scopes, diagnostics, source generator).
- VContainer Example sample: every VContainer feature with one LifetimeScope, one folder per feature, including tickable timer and bullet systems. The VContainer tab of Pretzel Setup generates and wires it.
