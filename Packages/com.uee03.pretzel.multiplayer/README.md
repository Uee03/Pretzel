# Pretzel Multiplayer

A session facade over Netcode for GameObjects (NGO) and Unity Multiplayer Services (Sessions + Relay). One interface,
`IMultiplayerSessionService`, covers local couch co-op, online host / join by code / quick join, cross-play tagging and
rejoin with backoff. A drop-in UI Toolkit menu drives it, and a pooled spawn handler recycles network prefabs.

## Requirements

- Unity 6000.0 or newer.
- `com.uee03.pretzel.core`, `com.uee03.pretzel.pooling` and `com.uee03.pretzel.ui` 0.2.0.
- `com.unity.netcode.gameobjects` 2.13.3
- `com.unity.services.multiplayer` 2.4.1
- `com.unity.services.authentication` 3.8.0
- `com.unity.services.core` 1.18.0
- `com.unity.modules.jsonserialize` and `com.unity.modules.uielements`.

Local mode needs no cloud project. Online mode needs the project linked to a Unity Cloud project with Authentication and
Multiplayer Services (Relay) enabled.

## Set up in about a minute

1. Install the package and its dependencies.
2. `GameObject > Pretzel > Multiplayer > Network Manager (Pretzel defaults)`. This adds a `NetworkManager`,
   `UnityTransport` and `PretzelNetworkBootstrap` with connection approval on, plus a network prefab list asset under
   `Assets/PretzelMultiplayer`.
3. `GameObject > Pretzel > Multiplayer > Menu`. This adds a `UIDocument` with the packaged layout and theme, a
   `PanelSettings` asset (under `Assets/PretzelMultiplayer`) and the `MultiplayerMenu` component.
4. On `PretzelNetworkBootstrap`, assign a **Player Prefab** (a `NetworkObject`, ideally with `NetworkPlayerIdentity`).
   Optionally add spawn points and pooled prefabs.
5. Press Play. The menu offers the modes below.

The Two Cubes sample builds a complete scene with these steps; see `Samples~/TwoCubes`.

## Modes

| Menu button | What it does |
|-------------|--------------|
| **Local co-op** | A loopback host on `127.0.0.1` (port `Loopback Port`) plus local players (the Local players slider, 1-4; values above `Max Local Players Per Client` are rejected, default 2). No cloud, no authentication, no remote clients. Each local player is a player object owned by the host client. |
| **Host online** | Creates a Sessions session with Relay, with a name, a max player count and an optional private flag (join code only). The lobby shows the join code. |
| **Join with code** | Joins by code. The same screen lists public sessions (**Refresh**) and joins the chosen one by id. |
| **Quick join** | Joins any open session through Sessions quick join (10 second timeout). It never creates a session, so it fails with an error when none is open. |

In the lobby guests press **Ready**, the host presses **Start match** once every other player is ready (local mode can
always start). Anyone can **Leave**. From code, use `PretzelNetworkBootstrap.Service`:

```csharp
IMultiplayerSessionService service = bootstrap.Service;
service.StateChanged += (previous, current) => Debug.Log(current);
bool started = await service.HostOnlineAsync(new HostRequest { DisplayName = "Ada", MaxPlayers = 4, SessionName = "Room" });
```

Operations report failures through `ErrorRaised` and a `false` result. They only throw for programming errors.
`ConnectionState` is `Disconnected`, `Connecting`, `InLobby`, `Playing`, `Reconnecting`, `ReconnectPrompt`,
`Disconnecting`.

## Cross-play

Every player and session carries a platform tag (`windows`, `macos`, `linux`, `android`, `ios`, `webgl`, `switch`,
`playstation`, `xbox`, `other`; see `PlatformTags`). Online mode itself is platform-agnostic (anonymous Authentication
plus Relay), so the tag is a label, not a capability.

- **Cross-play on** (the menu toggle): browsing and quick join show every session.
- **Cross-play off**: browsing and quick join only offer sessions created on the player's own platform tag. A session
  without a tag is still offered.
- Joining by code or by an id chosen from the list is an explicit choice and is never restricted.

## Rejoin

When an online **client** loses its connection, the service enters `Reconnecting` and retries with exponential backoff
(`NetworkSettings`: attempts, base delay, max delay, jitter 20%). If the attempts run out it enters `ReconnectPrompt` with
**Rejoin match** and **Return to lobby**. The same prompt is offered on the next start when a session was remembered
(`PretzelNetworkBootstrap` > Offer Rejoin On Start). `NotifyApplicationResumed` starts the flow when the connection died
while the app was suspended. A client that rejoins with the same player key gets its slots and objects back when the host
kept them during the grace window.

Limits:

- Only online clients can rejoin. A host's Relay allocation dies with the host, and a local session lives in the process,
  so losing the host connection ends the session.
- Not offered when the player was kicked, the host left or the session is gone or full.
- The remembered session expires after `Reconnect Record Max Age Minutes` (30 by default) and is stored in `PlayerPrefs`
  as identifiers only, never credentials.
- The host keeps a dropped player's slots and objects for `Reconnect Grace Seconds` (60 by default), then despawns them.
- The player key is the Authentication player id. It is not a credential: within a trusted co-op group another member
  could claim it during the grace window.

## NetworkSettings

A serializable class on `PretzelNetworkBootstrap`. Values are starting points for a small co-op game, not measured
recommendations. `Validate()` runs in `Awake`; invalid settings disable the bootstrap and log an error.

| Group | Fields |
|-------|--------|
| Netcode | `Tick Rate` (10-120, default 30), `Protocol Version` |
| Players | `Max Players` (default 4), `Max Local Players Per Client` (default 2), `Max Connection Data Bytes` (512), `Approval Timeout Seconds` (10), `Spawn Timing` (`OnConnect` or `OnMatchStart`, default `OnMatchStart`) |
| Reconnect | `Reconnect Grace Seconds`, `Reconnect Max Attempts` (5; 0 shows the prompt at once), base delay (1 s), max delay (15 s), record max age |
| Transport | connect timeout, max connect attempts and disconnect timeout in the transport's units; 0 keeps the `UnityTransport` default |
| Local and online | `Loopback Port` (7777), `Authentication Profile` (to run several players on one machine), `Allow Late Join` (when off, the session is locked while the match runs; existing players can still rejoin) |

## Pooled spawn handler

Add network prefabs to `PretzelNetworkBootstrap` > Pooled Prefabs (prefab, prewarm count, max idle). The bootstrap
registers a `PooledNetworkPrefabHandler` per entry, so NGO instantiates and destroys through a
`Pretzel.Pooling` `ComponentPool<NetworkObject>` on the server and on clients.

- The server spawns with `NetworkObject.InstantiateAndSpawn` and removes with `Despawn(true)`.
- On return the handler restores parent and scale, `SpawnWithObservers`, `DontDestroyWithOwner`, `AutoObjectParentSync` and
  `CheckObjectVisibility`, then every `IPoolable.OnReturnedToPool` runs.
- `NetworkVariable` values are not reset by NGO. Set initial values on the server in `OnNetworkSpawn` or `OnTakenFromPool`.
- Pooled instances stay at the scene root, because NGO rejects parenting a spawned object under a transform without a
  `NetworkObject`.

## Architecture

| Area | Types | Notes |
|------|-------|-------|
| `Runtime/Core` | `MultiplayerSessionService`, `ConnectionStateMachine`, `ISessionBackend`, `ExponentialBackoffPolicy`, `PlayerSlotRegistry`, `CrossPlayRules`, `LobbyRules`, `RequestValidator`, `ConnectionPayloadCodec` | Plain C# with no scene or backend types. Unit tested with fake backends and an injected clock. |
| `Runtime/Netcode` | `PretzelNetworkBootstrap`, `ServerPlayerSpawner`, `LocalSessionBackend`, `PooledNetworkPrefabHandler`, `NetworkPlayerIdentity` | The composition root and the NGO adapters. Connection approval, player spawning and the grace window live here. |
| `Runtime/Online` | `OnlineSessionBackend`, `UgsErrorMapper` | The only code that touches Multiplayer Services and Authentication. SDK errors become `MultiplayerException`. |
| `Runtime/UI` | `MultiplayerMenu`, `MultiplayerMenuPresenter`, `MultiplayerMenuView` | The presenter talks to `IMultiplayerSessionService` and `IMultiplayerMenuView` only. Uses `Pretzel.UI` and a USS theme. |
| `Editor` | `MultiplayerMenuItems`, `MultiplayerSceneBuilder` | The `GameObject > Pretzel > Multiplayer` menu items, built with supported Editor APIs. |

The session service owns one backend per session, created by a factory (`SessionMode.Local` or `Online`). Replace the
factory to plug in another transport.

## Samples

- `Samples~/TwoCubes`: two networked cubes that can only move, jump and turn, plus Spawn Item and Delete Items buttons on
  the pooled spawn handler. See its README.

## Not verified

- Hosting was run against a live project (Multiplayer Services 2.4.1): anonymous sign-in, a Relay session with a join
  code, the session query, ready, start and end match, and leaving (which deletes the session). Joining from a second
  client (by code, by id, quick join), reconnect and cross-play filters have not been run against a real service.
- No multi-platform or device testing. Cross-play only means that sessions carry a platform tag; no cross-platform join
  has been exercised.
- No performance measurement: the defaults in `NetworkSettings`, the pool sizes and the tick rate are not tuned or profiled.
