# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Fixed

- The menu's text inputs (display name, session name, join code, the number fields) showed the menu's light text on Unity's light input background, so typed text was hard to read. Inputs now use dark text on a light background, set by two new tokens, `--pretzel-multiplayer-input-text` and `--pretzel-multiplayer-input-background`.
- `NetworkPrefabListUtility.Register` no longer adds a prefab to a list asset twice when an Editor builder runs again. It checked only the NetworkManager's merged prefabs, which are empty on a fresh manager, so a repeated build gave NGO a duplicate `GlobalObjectIdHash` error at runtime.

## [0.2.0] - 2026-10-29

First release.

### Added

- `IMultiplayerSessionService` and `MultiplayerSessionService`: one facade for local couch co-op, online host, join by code or id, quick join, lobby ready flags, match start and end, and session queries. A connection state machine reports `Disconnected`, `Connecting`, `InLobby`, `Playing`, `Reconnecting`, `ReconnectPrompt` and `Disconnecting`.
- Cross-play tagging: platform tags on players and sessions, and cross-play rules that limit browsing and quick join to the own platform when cross-play is off.
- Rejoin with exponential backoff and jitter, a Rejoin / Return to lobby prompt, a remembered session in `PlayerPrefs` (identifiers only) and a host-side grace window that keeps a dropped player's slots and objects.
- `PretzelNetworkBootstrap`: applies `NetworkSettings` to the NetworkManager and UnityTransport, registers pooled prefab handlers and creates the session service. Server-side connection approval and player spawning with local player slots.
- `PooledNetworkPrefabHandler`: a Pretzel Pooling `ComponentPool` behind NGO's prefab instance handler.
- `OnlineSessionBackend` on Unity Multiplayer Services (Sessions + Relay) with anonymous Authentication, and `LocalSessionBackend` on loopback.
- Drop-in UI Toolkit menu (`MultiplayerMenu`, presenter, view, UXML, USS and theme) for mode choice, host, join, lobby, busy and rejoin screens.
- Editor menu items `GameObject > Pretzel > Multiplayer > Menu` and `Network Manager (Pretzel defaults)`.
- Sample: Two Cubes, with a scene creator under `Pretzel > Samples > Multiplayer`.
- EditMode and PlayMode tests.
