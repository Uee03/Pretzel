---
name: unity-netcode
description: "Design Unity multiplayer authority, replication, RPCs, prediction, reconciliation, network pooling, disconnect handling, and tests. Use for Netcode for GameObjects/Entities or existing Mirror, Fish-Net, Photon, and package network adapters without assuming a stack."
---

# Unity multiplayer netcode

## Workflow

1. Identify the existing network stack/version, transport, topology, player count, tick rate, targets,
   and latency/security needs. If none is selected, ask; the Multiplayer Center package alone does
   not establish a runtime netcode choice. Do not introduce NGO/Entities/Mirror/Photon implicitly.
2. Separate portable simulation rules from network serialization, transport, authority, and scene
   adapters. Define client/server/owner responsibilities and spawn/despawn/session lifetimes.
3. Classify data as durable replicated state, transient event, or client input. Late joiners need
   current state, not a history of transient RPC effects. Define reliability and bandwidth budgets.
4. Verify stack-specific APIs in installed source/docs. For NGO, check RPC attributes/targets and
   NetworkVariable permissions for the selected version; do not mix legacy ServerRpc/ClientRpc
   examples with newer unified RPC APIs without confirming support.
5. Validate inputs on the authority: sender ownership, ranges, rate limits, sequence/tick, and allowed
   transitions. Client-side validation/ownership flags are not a substitute for server validation.
6. Plan interpolation/prediction/reconciliation only where required. Address nondeterministic physics,
   floating-point/platform differences, clock/tick drift, and rollback side effects explicitly.
7. Network pooling requires approved spawn/despawn handlers and reset of network identity/state;
   ordinary SetActive pooling is insufficient. Cancel subscriptions/work at despawn/session exit.
8. Handle late joins, disconnects, ownership transfer, reconnect, scene synchronization, and failure
   paths. Packages should expose optional network adapters instead of depending on game managers.
9. Verify separate client/server instances under latency, packet loss, and disconnect; include late
   join and malicious/invalid input tests. Host-only testing misses many authority/timing defects.

## DO NOT

- Trust client transforms/damage/inventory blindly, transmit entire state every frame, or assume
  commands are deterministic just because they can be serialized.
- Assume a method named RPC has the same lifetime/ordering semantics across networking stacks.
- Claim offline or single-host tests validate multiplayer; do not choose a network stack without approval.

## Sources

- [Unity multiplayer documentation](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest/)
- [Netcode for Entities documentation](https://docs.unity3d.com/Packages/com.unity.netcode@latest/)
- [State transitions](../unity-design-patterns/reference/state.md)
- [Pool ownership](../unity-object-pooling/SKILL.md)

For other stacks, consult that project's version-matched vendor docs/source before writing API calls.
