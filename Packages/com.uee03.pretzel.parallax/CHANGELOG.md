# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- `ParallaxController` and `ParallaxLayer`: layers with a per-axis factor or a depth, endless horizontal and vertical repeating, and optional pixel snapping, all moved from one allocation-free `LateUpdate` (execution order 300, after the Cinemachine Brain).
- `ParallaxMath`: the factor, position, wrap and snap arithmetic as pure functions.
- `ParallaxMode.Depth`: derives the factor from the distance in front of the camera, so an orthographic 2.5D camera shows the parallax a perspective camera would. Layers are left to the projection under a perspective camera.
- *GameObject > Pretzel > Parallax > Parallax Background* menu entry.
- Sample **Parallax Example**: a 2D and a 2.5D scene on six layers of VectorCraft art, with the generator and sources in `Art~`.
- `ParallaxLayer.AutoScroll` and `ParallaxController.AutoScrollScale`: layers that scroll by themselves in units per second, for backdrops behind a camera that does not move. `Refresh(deltaTime)` advances them; `Refresh()` is unchanged.
- EditMode and PlayMode tests.
