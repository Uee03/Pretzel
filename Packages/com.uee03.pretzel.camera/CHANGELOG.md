# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- `CameraShake`: trauma-based screen shake as a Cinemachine extension, on unscaled time by default, allocation-free.
- `PlayfieldFitter`: keeps a playfield visible on every aspect ratio for orthographic and perspective cameras, with Contain, Cover, Width and Height modes and padding.
- `TraumaShake`, `PlayfieldFit` and `ICameraShaker`: the logic as plain C# and the interface effects depend on.
- *GameObject > Pretzel > Camera* menu entries and `CameraRigBuilder`: ready 2D, 2.5D and 3D Cinemachine rigs with Undo support.
- Sample **Camera Example** with VectorCraft art, a control panel and a scene builder.
- EditMode tests for the math and the rig builder, and PlayMode tests that run both extensions inside a live Cinemachine pipeline.
