# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- New package. `PretzelLifetimeScope`, `ScriptableInstaller`, `MonoInstaller`, their PlayMode tests, the guide and the VContainer Example sample moved here from `com.uee03.pretzel.integrations`, so a project can use VContainer without the API client, UI and tween packages that Integrations depends on. The package has no Pretzel dependencies.
- The guide starts with a comparison of ScriptableObject or bootstrap composition and a container, and links to the new "Without a container" guide.

### Changed

- The assembly (`Pretzel.Integrations.VContainer`), namespace and script GUIDs are unchanged from the Integrations package, so existing code, assembly references, prefabs and scenes keep working.
