# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Changed

- The generated VContainer registrations for the API client, pool registry, audio service and preferences service now also register their interfaces (`IApiClient`, `IPoolRegistry`, `IAudioService`, `IPreferencesService`) next to the concrete type.
- The scene templates message no longer mentions the removed Templates package.

## [0.2.0] - 2026-10-27

First published release. Version 0.1.0 was internal and never tagged.

### Added

- The Project Setup window: project info, folder presets, scene templates, first-run starter kit and the Addons and Addons Extra tabs.
- The VContainer tab: installs VContainer, generates the composition root code and creates the root scope, installer and VContainerSettings assets, and toggles VContainer diagnostics. It links to the Pretzel guide and the official VContainer documentation.
- VContainer tab starter services: ready-to-use `ISaveService`/`SaveService` and `IGameService`/`GameService`, and a `GameServicesExample`, generated from templates in `Editor/Templates/Composition`.
- The module catalog is generated from the module manifests (`.github/scripts/Update-ModuleCatalog.ps1`) into `Editor/Catalog/modules.json`, so Addons tab dependencies can no longer drift from `package.json`.
- EditMode tests that check the catalog against the installed manifests.
