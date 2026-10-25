---
name: unity-input-system
description: "Integrate Unity New Input System action maps, generated wrappers, PlayerInput, rebinding, multiplayer input ownership, and R3 adapters. Use for keyboard/gamepad/touch input, device switching, focus, or callback lifetime in 2D or 3D."
---

# Unity Input System

## Workflow

1. Read the existing action asset/wrapper and input adapter. Confirm the version-specific APIs
   without changing the project's default package set. Identify devices, players, and UI ownership.
2. Separate input intent from game rules and 2D/3D implementation. Pass typed values/commands to
   consumers; do not embed transform mutation or network authority in action callbacks.
3. Prefer the existing generated C# wrapper, PlayerInput, or explicit InputAction wiring pattern.
   Never edit generated wrapper source. Assign exactly one owner to enabling/disabling each map.
4. Specify started/performed/cancelled semantics and polling vs event needs. A callback's context
   should be consumed immediately, not retained for later use; capture its typed value if needed.
5. Unsubscribe matching handlers on disable/teardown and reset held input on focus loss or map changes.
   R3 wrapping needs owned disposal; event-driven input is not automatically leak-free.
6. For rebinding, manage operation lifetime, cancellation/timeout, composite parts, excluded controls,
   conflicts, and persistence of binding overrides. Never leave an active rebind after the UI closes.
7. For local multiplayer, keep each user's device pairing/actions separate. Check UI input-module
   wiring to avoid duplicate UI/gameplay consumption. Network input is a separate authority decision.
8. Test map switching, disable/enable, unplug/replug, focus loss, rebinding cancel, gamepad/keyboard,
   supported touch paths, and multiple players where applicable.

## DO NOT

- Mix legacy and new input paths by default, instantiate multiple action wrappers unintentionally,
  or retain callback contexts after invocation.
- Treat one global singleton input source as automatically correct for every player/package.
- Install a device/network package speculatively or choose 2D vectors vs 3D movement from the agent's preference.

## Sources

- [Input System documentation - choose installed version](https://docs.unity3d.com/Packages/com.unity.inputsystem@latest/)
- [Observer lifetime](../unity-design-patterns/reference/observer.md)
- [Command](../unity-design-patterns/reference/command.md)
