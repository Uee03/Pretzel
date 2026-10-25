---
name: unity-scene-loading
description: "Implement smooth Unity scene loading, additive transitions, activation gating, loading UI, fades, progress, cancellation, and recovery with SceneManager or installed Addressables/UniTask. Use for transition hitches, race conditions, or bootstrap lifetime."
---

# Unity scene loading

## Workflow

1. Identify the current scene flow, bootstrap ownership, Single vs Additive needs, loading backend,
   and memory budget. A reusable package receives scene requests/backends rather than game constants.
2. Define a serialized transition state machine: idle, fading out, loading, activating, initializing,
   fading in, failed. Decide whether duplicate requests queue, replace, or are rejected.
3. Display the loading view and allow it to render before expensive work. Use async loading via
   the existing backend; stage spawning/prewarming across frames to reduce main-thread spikes.
4. For SceneManager activation gating, understand the documented pre-activation progress plateau
   and queue blocking. Releasing `allowSceneActivation` must be part of recovery; gated loading
   must not wait on another queued scene operation that cannot run until activation proceeds.
5. Show phase-based or indeterminate progress when totals are unknown. Scene loading/download
   progress is not a trustworthy linear estimate of remaining time. Addressables byte progress
   and operation progress are different metrics; do not apply SceneManager normalization to both.
6. Activate at a safe boundary, set the active scene deliberately for additive flows, wire new
   services, and transfer ownership before unloading old resources. Track peak overlap memory.
7. Define cancellation honestly: the await can be cancelled while the Unity operation still runs.
   Observe completion, resolve activation, and unload/release unwanted scenes through the proper API.
   Recover loading UI and input state even on errors; do not leave a black screen or gated queue.
8. Keep fades/input suppression alive through the transition; bind UniTask/LitMotion to the actual
   persistent transition owner, not the scene being unloaded.
9. Test rapid repeated requests, missing scene, interruption, additive unload, scene initialization
   failures, low-memory overlap, and frame-time spikes in a built player.

## DO NOT

- Promise hitch-free loading because an API is async; Awake/OnEnable, activation, and initialization
  may still take main-thread time.
- Use arbitrary delays as readiness signals, unload the loading owner early, or abandon a gated load.
- Assume every scene uses a build-index or the same Addressables acquisition/release contract.

## Sources

- [SceneManager.LoadSceneAsync](https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html)
- [allowSceneActivation](https://docs.unity3d.com/ScriptReference/AsyncOperation-allowSceneActivation.html)
- [Addressables handles](../unity-addressables/SKILL.md)
- [State pattern](../unity-design-patterns/reference/state.md)
