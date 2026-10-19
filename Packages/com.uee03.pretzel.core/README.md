# Pretzel Core

Shared foundations for every Pretzel module. No dependencies.

## Contents

- `Guard`: argument checks that throw with the parameter name. `NotNull`, `NotDestroyed`, `NotNullOrEmpty`, `InRange`.
- `Singleton<T>`: scene-bound singleton. A second instance destroys itself.
- `PersistentSingleton<T>`: survives scene loads.
- `RegulatorSingleton<T>`: the newest instance replaces the old one.

```csharp
public class GameSession : Singleton<GameSession> { }

GameSession.Instance.DoSomething();
```

Singletons are never created in edit mode or while the application quits. Use `HasInstance` or `TryGetInstance()` to check without creating one. When you override `Awake` or `OnDestroy`, call the base method.

## Sample

`Samples~/CoreUsage`: a game session, a music player and a scene director.
