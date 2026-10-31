# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [0.2.0] - 2026-10-29

First release.

### Added

- `ICloudAuthService`, `ICloudSaveService`, `ICloudCodeService` and `IRemoteConfigService`, which reference no SDK, with `CloudResult`, `CloudError` and `CloudErrorKind` so callers never see SDK exceptions.
- `RetryPolicy` and `CloudRetry`: exponential backoff with jitter, `RetryAfter` support and cancellation.
- `SyncedDataStore`, an offline-first `IDataStore` over a local store and Cloud Save: persistent upload queue, ordered flush, newest-wins, keep-local and keep-cloud conflict resolvers, schema migration of cloud payloads, and `SyncStatus`.
- `CloudServices` and `CloudServicesBuilder` for manual composition.
- Adapter assemblies for Unity Authentication (3.8.0), Cloud Save (3.4.2), Cloud Code (2.10.4, below 3.0.0) and Remote Config (4.2.5), each compiled only when its package is installed.
- In-memory fakes with failure injection in `Pretzel.Cloud.InMemory`.
- Samples: Cloud Save Usage, Cloud Code And Remote Config and Manual Wiring With Fakes.
- EditMode tests that need no Unity Gaming Services package and no network.
