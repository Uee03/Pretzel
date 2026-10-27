# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [0.2.0] - 2026-10-27

First published release. Version 0.1.0 was internal and never tagged.

### Added

- The Project Setup window: project info, folder presets, scene templates, first-run starter kit and the Addons and Addons Extra tabs.
- The module catalog is generated from the module manifests (`.github/scripts/Update-ModuleCatalog.ps1`) into `Editor/Catalog/modules.json`, so Addons tab dependencies can no longer drift from `package.json`.
- EditMode tests that check the catalog against the installed manifests.
