# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- `IPreferencesService`, the contract consumers depend on. `PreferencesService` implements it, so other backends (PlayerPrefs, a platform or cloud service) can be swapped in.
- Samples: Custom Preferences Service (PlayerPrefs and in-memory implementations) and Options Menu (runtime UI Toolkit options screen, Apply/Revert draft and appliers).

## [0.2.0] - 2026-10-27

First published release. Version 0.1.0 was internal and never tagged.

### Added

- `PreferencesService` and `PlayerPreferences` with audio, graphics, controls and language sections, saved through Pretzel Data.
- EditMode tests.
