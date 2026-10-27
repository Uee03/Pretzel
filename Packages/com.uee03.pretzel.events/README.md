# Pretzel Events

Decoupled messaging in two flavours. Depends on Pretzel Core.

## EventBus

A typed in-memory bus. Own one per game or scope and pass it in (Pretzel DI can register it as a singleton).

```csharp
public readonly struct EnemyDefeated { public readonly int Points; public EnemyDefeated(int p) => Points = p; }

EventBus bus = new();
IDisposable subscription = bus.Subscribe<EnemyDefeated>(e => score += e.Points);
bus.Publish(new EnemyDefeated(10));
subscription.Dispose();
```

- Handlers are stored copy-on-write, so publishing does not allocate and subscribing during a publish is safe; the
  change applies to the next publish.
- An exception in one handler is logged and the other handlers still run.
- Use it from the main thread.

## ScriptableObject channels

Assets that designers wire in the Inspector: **Assets > Create > Pretzel > Events**. `VoidEventChannel` has no payload;
`IntEventChannel`, `FloatEventChannel`, `BoolEventChannel`, `StringEventChannel`, `Vector3EventChannel` and
`GameObjectEventChannel` carry one. Derive from `EventChannel<T>` for your own payload type.

Listener components (`VoidEventListener`, `IntEventListener`, `FloatEventListener`, `BoolEventListener`,
`StringEventListener`) forward a channel to a UnityEvent while enabled. Derive from `EventChannelListener<TChannel, T>`
for your own channel.

Channels outlive scenes. Subscribe in `OnEnable` and dispose in `OnDisable`. Turn on **Log Raises** on a channel
while wiring a scene.

## Sample

`Samples~/EventsUsage`: a bus with a struct message and a channel raised with the score.
