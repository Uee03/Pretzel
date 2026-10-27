# Pretzel DI

A small dependency injection container for Unity. Depends on Pretzel Core. Use it when a project has outgrown
singletons and scene searches; for a large project with source generators and an existing team convention,
VContainer (in the Addons Extra tab) is the heavier alternative.

## Registering and resolving

```csharp
ContainerBuilder builder = new();
builder.Register<IClock, SystemClock>(Lifetime.Singleton);
builder.Register<ScoreService>(Lifetime.Scoped).AsSelf().AsImplementedInterfaces();
builder.RegisterInstance(settingsAsset);                         // Unity objects and existing objects
builder.RegisterFactory<ILogger>(r => new UnityLogger("Game"));  // anything the container cannot construct
builder.RegisterEntryPoint<WaveDirector>();                      // IStartable, ITickable, ILateTickable, IFixedTickable

Container container = builder.Build();   // validates constructor dependencies now, not on first use
ScoreService score = container.Resolve<ScoreService>();
```

| Lifetime | Instances |
|---|---|
| `Transient` | A new one per resolve. |
| `Scoped` | One per scope (the root container is a scope). |
| `Singleton` | One for the registering container and its child scopes. It resolves its own dependencies from that container. |

- Constructor injection picks the `[Inject]` constructor, otherwise the public constructor with the most parameters.
  Parameters with a default value may be left unregistered.
- `[Inject]` fields, properties and methods (also private, also in base classes) are filled by `Inject(target)`.
  Use them for MonoBehaviours, which cannot have constructors.
- Duplicate registrations in one scope, missing dependencies and Unity object types throw `ContainerException` from
  `Build`. Circular dependencies throw on resolve.
- Disposing a container disposes its child scopes and every `IDisposable` it constructed, newest first. Registered
  instances and factory results are not disposed; their creator owns them.
- `CreateScope(b => ...)` adds registrations on top of the parent; a child registration hides the parent's.

## In a scene

`LifetimeScope` is the composition root component. It builds in `Awake` (execution order -5000) from `Configure`, the
`ScriptableInstaller` assets in its list and every `IInstaller` component on its GameObject. It then injects its own
hierarchy and the **Auto Inject** objects, starts the entry points and ticks them. Set **Parent** to nest scopes, for
example a scene scope under a project scope.

```csharp
public sealed class GameScope : LifetimeScope
{
    protected override void Configure(ContainerBuilder builder)
    {
        builder.Register<ScoreService>(Lifetime.Singleton);
    }
}
```

Instantiate prefabs through the container so they are injected before `Awake`:
`Container.Instantiate(prefab, position, rotation)`. Inject existing objects with `Container.InjectGameObject(go)`.

## IL2CPP and stripping

`[Inject]` derives from `PreserveAttribute`, so injected members survive stripping. Constructors are invoked by
reflection; mark a type's constructor `[Inject]` or `[Preserve]`, or list the type in `link.xml`, if a high stripping
level removes it.

## Sample

`Samples~/DIUsage`: a scope with a service, a ticking entry point and an injected MonoBehaviour.
