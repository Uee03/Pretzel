# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- `SfxLibrary`, `SfxEntry`, `SfxPlayer`, `ISfxPlayer` and `SfxThrottle`: sounds by name with a per-sound cooldown and voice limit, on top of Pretzel Audio.
- Sample **SFX Pack**: thirteen sounds generated with SoundCraft, their cues and library, and a sound board scene. The generator and sources are in `Audio~`.
- EditMode tests with a fake audio service, and PlayMode tests for the voice limit on the real `AudioService`.
