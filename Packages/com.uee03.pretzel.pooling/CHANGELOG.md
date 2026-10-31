# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- `IComponentPool<T>`, `IPoolRegistry` and `IPoolFactory<TKey, T>`, implemented by `ComponentPool<T>`, `PoolRegistry` and `PoolFactory<TKey, T>`, so spawners can be tested against fakes.
- Samples: Enemy Waves (keyed factory, caps, return callbacks, metrics), Bullets And Impacts (`PoolRegistry` plus an effect-spawner interface) and Floating Text (pooled UI Toolkit labels).

### Fixed

- `PoolRegistry` threw `InvalidCastException` when the same prefab component was requested under two component types (for example `Spawn(GameObject)` and `GetPool<Component>`); pools are now keyed by prefab and type.

## [0.2.0] - 2026-10-27

First published release. Version 0.1.0 was internal and never tagged.

### Added

- `ComponentPool<T>` on `UnityEngine.Pool` with prewarming and side-effect-free creation, `IPoolable` hooks, self-releasing `PooledObject` and `PoolRegistry`.
- `PoolFactory<TKey, T>`: Inspector-configured pools by key (for example an enum) with up-front validation, per-key prewarm, grouped Hierarchy containers and runtime registration, plus serializable `PoolSettings`.
- EditMode tests.
