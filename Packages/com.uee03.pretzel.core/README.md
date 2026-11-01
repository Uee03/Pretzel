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

## Timers

Plain C# timers in `Pretzel.Core.Timers`. The owner calls `Tick(deltaTime)`, so it chooses scaled, unscaled or fixed
time, and the timers are easy to test.

| Type | Use |
|---|---|
| `CountdownTimer` | Counts down from a duration, raises `Completed` once. `Start`, `Pause`, `Resume`, `Reset`, `Progress`. |
| `StopwatchTimer` | Accumulates elapsed time while running. |
| `Cooldown` | `TryUse()` succeeds when ready and starts the cooldown. `RemainingNormalized` drives a radial fill. |
| `IntervalTimer` | Raises `Elapsed` every interval and catches up when one tick spans several intervals. |
| `AwaitableDelay` | `SecondsAsync(seconds, unscaled, token)` and `FramesAsync(frames, token)` on `Awaitable`. |
| `FrameBudget` | `await budget.YieldIfExceededAsync(token)` between work items waits a frame once this frame's milliseconds are used, so spawning or loading never freezes the game. |

```csharp
Cooldown dash = new(0.75f);

void Update()
{
    dash.Tick(Time.deltaTime);
    if (dashPressed && dash.TryUse()) { Dash(); }
}
```

`DisposableAction` runs an action once on `Dispose`, which turns any unregister call into an `IDisposable`.

`CompositeDisposable` is a bag for the subscriptions an owner makes. Add each `IDisposable` a subscribe call returns, then dispose the bag once; items are disposed in reverse order, and one that throws is logged without stopping the rest. `Clear()` disposes the items and keeps the bag usable (subscribe on enable, clear on disable), and an item added after `Dispose` is disposed at once.

```csharp
private readonly CompositeDisposable _subscriptions = new();

void Start()
{
    _bus.Subscribe<InvaderDestroyed>(OnDestroyed).AddTo(_subscriptions);
    _bus.Subscribe<WaveCleared>(OnCleared).AddTo(_subscriptions);
}

void OnDestroy() => _subscriptions.Dispose();
```

## Sample

`Samples~/CoreUsage`: a game session, a music player and a scene director.
