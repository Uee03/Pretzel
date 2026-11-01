# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- `TimeScaleService`, `ITimeScale` and `TimeScaleStack`: hit stop and slow motion from a stack of self-expiring requests, on unscaled time, that cooperates with a pause and always restores the time.
- `SpriteFlash` and `PunchScale`: effects that run only while playing, capture the original at the start of an effect and restore it exactly, on unscaled time.
- `FeelMath`: the flash and punch curves as pure functions.
- Sample **Game Feel Example** with VectorCraft art, a control panel and a scene builder.
- EditMode tests for the math and the stack, and PlayMode tests for the service, the pause interplay and both effects.
