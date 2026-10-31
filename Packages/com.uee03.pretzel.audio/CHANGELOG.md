# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- `IAudioService`, implemented by `AudioService`, for gameplay code that should not depend on the component.

### Changed

- **Breaking:** `AudioManager` is renamed `AudioService` (file `AudioService.cs`, menu **Add Component > Pretzel > Audio > Audio Service**), because it implements `IAudioService`. The script keeps its `.meta` GUID, so scenes and prefabs that use it stay linked; only code that names the type, such as `[SerializeField] AudioManager`, needs the new name. The pooled GameObjects it creates are named `Emitter <n>`.
- The internal pooled-source type `AudioService.Voice` (formerly `AudioManager.Voice`) is now the separate internal class `AudioEmitter` (`AudioEmitter.cs`). Serialized fields (`Max Voices`, `Prewarm Voices`) and the `AudioChannel.Voice` dialogue channel are unchanged.

### Fixed

- `StopMusic` (or `PlayMusic(null)`) during a crossfade left the outgoing track looping with no way to stop it.
- `PlayMusic` with the clip that already plays now applies the new `volume` immediately instead of at the next `SetVolume`.

## [0.2.0] - 2026-10-27

First published release. Version 0.1.0 was internal and never tagged.

### Added

- `AudioManager` with pooled voices, voice stealing, stale-safe `AudioHandle`s, channel volumes through an `AudioMixer` or directly, and music crossfades.
- `AudioCue` assets and `AudioMath` decibel conversion.
- EditMode tests.
