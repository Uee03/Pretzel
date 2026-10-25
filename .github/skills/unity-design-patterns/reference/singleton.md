# Singleton

Use only for a deliberately unique service with a documented scope. Prefer an explicitly owned
composition-root instance passed through interfaces to consumers.

- Distinguish process, game session, scene, and player scope; multiplayer does not imply one player.
- If an existing singleton base is reused, inspect duplicate detection, initialization, static
  reset, destruction, and domain-reload-disabled behavior before depending on it.
- Avoid creating GameObjects as a getter side effect. `DontDestroyOnLoad` does not establish
  dependency order or prevent duplicate bootstrap instances.
- Test additive scenes, second play sessions, teardown, and headless/package consumers.

Do not use Singleton as a service locator, a global event bus, or an excuse to make code untestable.
Runtime packages should not silently create or require a scene-level singleton.