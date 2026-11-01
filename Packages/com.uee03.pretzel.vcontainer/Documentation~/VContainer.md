# VContainer with Pretzel

[VContainer](https://vcontainer.hadashikick.jp) is a dependency injection (DI) container for Unity. With it you register
the objects your game needs in one place, and they receive what they depend on through their constructors. Pretzel
gives you three runtime types and a setup tool, so a new game starts with a working composition root in one click.

| Part | Where | What it does |
|------|-------|--------------|
| `PretzelLifetimeScope` | Pretzel VContainer | A `LifetimeScope` configured in the Inspector: settings assets, installer assets and scene installers. |
| `ScriptableInstaller` | Pretzel VContainer | Base class for registrations kept in an asset (project-wide). |
| `MonoInstaller` | Pretzel VContainer | Base class for registrations that need scene references (per scene). |
| VContainer tab | Pretzel > Project Setup | Installs VContainer, generates the installer, services and flow, and creates the root scope. |

The runtime types compile only when VContainer (`jp.hadashikick.vcontainer`) is installed. The **VContainer Example**
sample (Package Manager > Pretzel VContainer > Samples) shows every VContainer feature in one scene, one folder per
feature.

**A container is optional.** Every other Pretzel module works without one. If you share services between objects and
scenes with ScriptableObjects or a plain bootstrap class, read [Without a container](../../com.uee03.pretzel.setup/Documentation~/WithoutAContainer.md)
first, and choose with the comparison below.

## Do you need a container?

Two common ways to share services and managers between objects and scenes:

1. **ScriptableObject assets (or a bootstrap class) that hold the services.** Designers wire them in the Inspector, any
   object can use them, and nothing but Unity is involved. This is a good fit for small and medium projects and for
   data that designers own.
2. **A composition root with a container.** Objects state what they need in their constructor and the container supplies it.

| | ScriptableObject or bootstrap | Container (VContainer) |
|---|---|---|
| Learning curve | Low: Unity concepts only | Higher: scopes, lifetimes, injection |
| Wiring | Drag assets into fields | Registrations in code and installers |
| Dependencies are visible | Only if you read the fields | In every constructor |
| Runtime state in assets | Persists in the Editor between Play sessions when domain reload is off, so reset it on enable | Services are plain objects created fresh each session |
| Order of startup | You control it with a bootstrap and execution order | The root scope is built before the first scene |
| Tests | Assets can be created in a test; fakes need an interface | Construct the class with fakes |
| Cleanup | Manual | The container disposes what it created |

Choose a container when the game grows many services that depend on each other, or when you want every dependency
visible and every class constructible in a test. Stay with assets when a handful of shared services is enough. You can
also mix them: register the configuration assets as settings of a scope (see below) and keep designers' wiring.

If you move from the first style to a container, what stays is what already worked: serializable classes for
configuration and ScriptableObjects for authored data. The object that owns startup becomes the root `LifetimeScope`
prefab, and its logic moves into a plain `GameFlow` class.

```
ScriptableObject style                   Container style
GameManager (MonoBehaviour)              GameLifetimeScope.prefab (created before the first scene)
  creates managers                         GameInstaller.asset registers services and managers
  fills ManagerContainer (SO)              GameFlow (IStartable) runs the game's startup
Objects read ManagerContainer.Audio      Objects receive IAudioService in their constructor
```

## Quick start

1. Open **Pretzel > Project Setup > VContainer**. Click **Install VContainer** if it is missing, and install
   **Pretzel VContainer** from the Addons tab.
2. Check the namespace, scope name and folders. Keep **Starter services** ticked for a ready-to-use save and game
   service. Tick the Pretzel services you use.
3. Add your own services and managers. Choose a lifetime, whether they get an interface, and whether they run
   `Start` (`IStartable`) or `Tick` (`ITickable`).
4. Click **Generate and set up**. After compilation the tool creates `GameInstaller.asset`,
   `GameLifetimeScope.prefab` and `VContainerSettings.asset`, and adds the settings to the preloaded assets.
5. Press Play in any scene. The root scope appears under DontDestroyOnLoad and `GameFlow.Start` runs. With the usage
   example ticked, the Console shows one short game being played, scored and saved.

Generated files:

| File | Owner | Rewritten |
|------|-------|-----------|
| `GameInstaller.Generated.cs` | The tool | On every run. Do not edit. |
| `GameInstaller.cs` | You | Never. Add registrations the tool does not cover here. |
| `GameFlow.cs`, `IInventoryService.cs`, `InventoryService.cs` | You | Never, created only when missing. Services you add are empty classes to fill in. |
| `ISaveService.cs`, `SaveService.cs` | You | Never. Complete: any serializable type by `DataKey` (slots, folders), sync and async, through Pretzel Data. A corrupt file throws and is kept, never overwritten. If the project already has these files (the Pretzel Data examples create the same ones), they are reused. |
| `IGameService.cs`, `GameService.cs`, `GameState.cs`, `GameProgress.cs` | You | Never. Complete: menu, playing, paused and game-over states, score, pause through `Time.timeScale`, saved high score and games played. Call `LoadProgressAsync` once at startup; progress is only saved after a successful load. `ProgressLoaded` is raised when it succeeds (check `IsLoaded` first if you subscribe late). |
| `GameServicesExample.cs` | You | Never. Shows the calls; plays one session on every start. Untick **Usage example** and delete it when done. |

Using the starter services from your own code:

```csharp
public sealed class HudPresenter : IStartable, IDisposable
{
    private readonly IGameService _game;
    private readonly HudView _view;

    public HudPresenter(IGameService game, HudView view)
    {
        _game = game;
        _view = view;
    }

    public void Start()
    {
        _game.ScoreChanged += _view.ShowScore;
        _view.PauseClicked += _game.Pause;
    }

    public void Dispose()
    {
        _game.ScoreChanged -= _view.ShowScore;
        _view.PauseClicked -= _game.Pause;
    }
}
```

Save anything serializable with `ISaveService`: `_save.Save(new DataKey("settings"), options)`,
`await _save.LoadOrCreateAsync<Options>(new DataKey("settings"))`, or per slot with `new DataKey("run", slot: 1)`.

If the code folder belongs to an assembly definition, add references to `VContainer`,
`Pretzel.Integrations.VContainer` and the Pretzel modules you register; the tab lists them.

## Scopes and lifetimes

```
GameLifetimeScope (root, DontDestroyOnLoad)   singletons for the whole session
 └─ LifetimeScope in a level scene            scoped services for that level, disposed when it unloads
```

| Lifetime | One instance per | Use for |
|----------|------------------|---------|
| `Singleton` | Container that registered it, shared with child scopes | Session services: saving, audio, scene loading, the event bus. |
| `Scoped` | Scope (each level scope gets its own) | Level state: spawners, wave managers, level HUD presenters. |
| `Transient` | Resolve | Small stateless helpers. Rarely needed. |

A scene that needs injection gets its own scope: **Add a scope to the open scene** in the tab, or add
`PretzelLifetimeScope` to a GameObject. Without a parent set, it uses the root scope as its parent.

## Services and managers

Constructor injection, interfaces at boundaries, no MonoBehaviour:

```csharp
public interface ISaveService
{
    void Save(Profile profile);
}

public sealed class SaveService : ISaveService
{
    private readonly IDataStore _store;

    public SaveService(IDataStore store) => _store = store;

    public void Save(Profile profile) => _store.Save("profile", profile);
}

public sealed class QuestManager : IQuestManager, IStartable, IDisposable
{
    private readonly ISaveService _save;
    private readonly IEventBus _events;
    private IDisposable _subscription;

    public QuestManager(ISaveService save, IEventBus events)
    {
        _save = save;
        _events = events;
    }

    public void Start() => _subscription = _events.Subscribe<EnemyDefeated>(OnEnemyDefeated);

    public void Dispose() => _subscription?.Dispose();

    private void OnEnemyDefeated(EnemyDefeated message)
    {
    }
}
```

Registration (generated, or by hand in `GameInstaller.cs`):

```csharp
builder.Register<ISaveService, SaveService>(Lifetime.Singleton);
builder.RegisterEntryPoint<QuestManager>(Lifetime.Singleton);
```

`RegisterEntryPoint` registers every interface of the class and runs `IStartable.Start`, `ITickable.Tick` and the
other [entry point interfaces](https://vcontainer.hadashikick.jp/integrations/entrypoint) on Unity's PlayerLoop.
VContainer inserts one PlayerLoop system per phase and calls every tickable from a plain C# loop, so one system that
moves 500 bullets or advances every timer is cheaper than 500 `MonoBehaviour.Update` calls. Tickables keep running
while their scope lives, regardless of GameObject state; pause them with a flag or dispose the scope.

### The flow class replaces the GameManager logic

```csharp
public sealed class GameFlow : IAsyncStartable
{
    private readonly SceneLoader _scenes;
    private readonly PreferencesService _preferences;

    public GameFlow(SceneLoader scenes, PreferencesService preferences)
    {
        _scenes = scenes;
        _preferences = preferences;
    }

    public async Awaitable StartAsync(CancellationToken cancellation)
    {
        await _preferences.LoadAsync(cancellation);
        await _scenes.LoadAsync("MainMenu", cancellationToken: cancellation);
    }
}
```

`IAsyncStartable.StartAsync` returns `Awaitable`, or `UniTask` when UniTask is installed.

## Configuration with ScriptableObjects

Keep authored values in ScriptableObjects and serializable classes, and register the asset:

```csharp
[CreateAssetMenu(menuName = "Game/Combat Settings")]
public sealed class CombatSettings : ScriptableObject
{
    [SerializeField] private float _critMultiplier = 2f;

    public float CritMultiplier => _critMultiplier;
}
```

Drag the asset into **Settings** on `GameLifetimeScope.prefab`. Any class can then take `CombatSettings` in its
constructor. Settings are registered by their concrete type; to resolve one through an interface, register it in
`GameInstaller.cs` with `builder.RegisterInstance<ICombatSettings>(settings)`.

## Scenes, MonoBehaviours and prefabs

Views stay MonoBehaviours. Register the scene objects a scope needs with a `MonoInstaller` on that scope:

```csharp
public sealed class LevelInstaller : MonoInstaller
{
    [SerializeField] private LevelHud _hud;
    [SerializeField] private EnemySpawnPoints _spawnPoints;

    public override void Install(IContainerBuilder builder)
    {
        builder.RegisterComponent(_hud);
        builder.RegisterComponent(_spawnPoints);
        builder.RegisterEntryPoint<LevelPresenter>(Lifetime.Scoped);
    }
}
```

`LevelPresenter` takes `LevelHud`, the level services and the session services in its constructor and wires them
together. If a MonoBehaviour itself needs a service, give it a method injection point:

```csharp
public sealed class ShopButton : MonoBehaviour
{
    private IShopService _shop;

    [Inject]
    public void Construct(IShopService shop) => _shop = shop;
}
```

It is injected when it is registered with `RegisterComponent`, listed in the scope's **Auto Inject Game Objects**, or
created with `resolver.Instantiate(prefab)`.

### Loading a level with its own scope

Pretzel's `SceneLoader` works unchanged. To give a level scope extra data, enqueue it while the scene loads:

```csharp
using (LifetimeScope.Enqueue(builder => builder.RegisterInstance(levelData)))
{
    await _scenes.LoadAsync(levelData.SceneName, options, cancellation);
}
```

The level's `PretzelLifetimeScope` picks up the root scope as parent and the enqueued `levelData` as a registration.
Use `LifetimeScope.EnqueueParent(scope)` in the same way to parent it to a scope other than the root.

### Injecting the current scope

Every scope registers itself as `LifetimeScope`. A class that creates scopes takes it in its constructor and gets the
scope it was registered in. This parents a loaded scene to a level scope instead of the root:

```csharp
public sealed class RoomLoader
{
    private readonly LifetimeScope _scope;
    private readonly SceneLoader _scenes;

    public RoomLoader(LifetimeScope scope, SceneLoader scenes)
    {
        _scope = scope;
        _scenes = scenes;
    }

    public async Awaitable LoadAsync(string room, CancellationToken cancellation)
    {
        using (LifetimeScope.EnqueueParent(_scope))
        {
            await _scenes.LoadAsync(room, new SceneLoadOptions { Mode = LoadSceneMode.Additive }, cancellation);
        }
    }
}
```

The room is loaded additively so the level scene, and the scope it holds, stays loaded.

This is the one place where depending on a VContainer type is right: the class's job is creating scopes.

### Child scopes without a scene

A match, a popup or a procedurally built level often has no scene of its own. Create a child scope in code and
dispose it when it ends:

```csharp
public sealed class MatchRunner
{
    private readonly LifetimeScope _scope;
    private readonly MatchInstaller _installer;
    private LifetimeScope _match;

    public MatchRunner(LifetimeScope scope, MatchInstaller installer)
    {
        _scope = scope;
        _installer = installer;
    }

    public void Begin() => _match = _scope.CreateChild(_installer, "Match");

    public void End()
    {
        _match?.Dispose();
        _match = null;
    }
}
```

`MatchInstaller` is a `ScriptableInstaller` asset listed in the root scope's **Settings**, so it can be injected.
`CreateChild` also accepts a lambda (`builder => builder.RegisterInstance(matchData)`), and
`CreateChildFromPrefab(prefab, installer)` instantiates a scope prefab with its own installers and scene objects.
`Dispose` disposes every service the child created and destroys its GameObject. Scoped entry points registered in
the child start when it is created and stop when it is disposed.

## Testing

Services are plain classes, so most tests need no container and no scene:

```csharp
[Test]
public void Saving_WritesTheProfile()
{
    FakeDataStore store = new();
    SaveService service = new(store);

    service.Save(new Profile { name = "Ada" });

    Assert.That(store.Saved, Is.Not.Empty);
}
```

To test wiring, build a container in the test:

```csharp
ContainerBuilder builder = new();
builder.Register<ISaveService, SaveService>(Lifetime.Singleton);
builder.RegisterInstance<IDataStore>(new FakeDataStore());
using IObjectResolver container = builder.Build();
Assert.That(container.Resolve<ISaveService>(), Is.Not.Null);
```

## Bad practices and how to avoid them

**Resolving from the container inside your classes (service locator).**

```csharp
// Bad: the dependency is hidden, and the class only works with a container.
public sealed class Shop
{
    private readonly IObjectResolver _resolver;
    public Shop(IObjectResolver resolver) => _resolver = resolver;
    public void Buy() => _resolver.Resolve<IWallet>().Pay(10);
}

// Good: the constructor states what the class needs.
public sealed class Shop
{
    private readonly IWallet _wallet;
    public Shop(IWallet wallet) => _wallet = wallet;
    public void Buy() => _wallet.Pay(10);
}
```

Only installers and factories may touch `IContainerBuilder` and `IObjectResolver`.

**Static singletons next to the container.** A `MusicService.Instance` bypasses the container: tests cannot replace
it and its lifetime is not managed. Register it and inject it (or its interface) instead.

**Runtime state in ScriptableObjects.** An asset field that changes during play (`currentHealth`, a list of active
quests) survives between Editor Play sessions and is shared by every scene. Keep it in a registered service; keep
assets read-only.

**A longer-lived object holding a shorter-lived one (captive dependency).**

```csharp
// Bad: a session singleton keeps the first level's spawner alive and uses it after the level unloads.
builder.Register<ScoreService>(Lifetime.Singleton);   // ScoreService(WaveSpawner spawner)
// in the level scope:
builder.Register<WaveSpawner>(Lifetime.Scoped);
```

A service may depend only on services that live as long as it does, or longer. Let the level talk to the session
service (`score.Add(points)`), or send a message on `IEventBus`.

**Using injected members in `Awake`.** Method injection runs when the scope builds. Use injected services from
`Start` or later, or move the logic into an entry point.

**`Object.Instantiate` for prefabs that need injection.** The copy is not injected. Use `resolver.Instantiate(prefab)`
in a factory, or pass the values the instance needs when you spawn it. For pooled objects prefer the second: a
spawner with injected services calls `enemy.Init(target, stats)`, and the pooled enemy needs no container.

**Registering everything as Transient.** A transient service with state gives every consumer its own copy, so
changes seem to disappear. Default to `Singleton` or `Scoped`, and use `Transient` only for stateless helpers.

**Heavy or asynchronous work in constructors.** Constructors run while the container builds, before the first frame.
Do loading in `IAsyncStartable.StartAsync` or an explicit `LoadAsync` method.

**An interface for every class.** Add interfaces where you substitute implementations (save backends, platform
services, tests). Data classes, settings and presenters do not need one.

**`RegisterInstance` for objects that need cleanup.** The container does not dispose instances it did not create.
Use `builder.Register(_ => new PoolRegistry(), Lifetime.Singleton)` so it is disposed with the scope.

**Circular dependencies.** `A(B)` and `B(A)` cannot be built. Usually one of them should publish an event on
`IEventBus`, or a third class should hold the shared logic.

## Diagnostics

Turn on **Enable diagnostics** in the VContainer tab (it sets `EnableDiagnostics` on `VContainerSettings`) and open
**Window > VContainer Diagnostics**. During Play it lists every scope, its registrations, the instances created and
who resolved them. Use it when a registration is missing or a service is created more often than expected.
Collection is not limited to the Editor and slows resolving, so turn it off before a release build; the tab shows a
warning while it is on.

## Builds and IL2CPP

VContainer builds objects through reflection. With Managed Stripping Level set to High, constructors used only by
the container can be stripped. Mark them with `[Inject]` (it counts as a preserve attribute) or keep the assembly
in a `link.xml`. Test a player build after adding services.

### Source generator

VContainer's [source generator](https://vcontainer.hadashikick.jp/optimization/source-generator) writes the
construction and injection code at compile time, which makes resolving faster and preserves the generated types
from stripping. It is not part of the package:

1. Download `VContainer.SourceGenerator.dll` from the [VContainer releases](https://github.com/hadashiA/VContainer/releases)
   for the installed version and put it under `Assets/`.
2. In its import settings, turn off **Any Platform**, **Editor** and **Standalone**, and add the asset label
   `RoslynAnalyzer`.

It applies to classes in assemblies that reference `VContainer`, that are registered with a `Register*` call or
have `[Inject]`. Nested classes, structs and private types fall back to reflection.

## Troubleshooting

| Symptom | Cause |
|---------|-------|
| `VContainerException: No such registration of type X` | X is not registered in this scope or a parent, or a scene scope is parented to a different scope. |
| Root scope not created in Play mode | `VContainerSettings` is missing from **Project Settings > Player > Preloaded Assets**. Run **Generate and set up** again. |
| A MonoBehaviour's injected field is null | It is not registered, not listed in Auto Inject Game Objects, or it was created with `Object.Instantiate`. |
| `Start`/`Tick` never runs | The class was registered with `Register` instead of `RegisterEntryPoint`. |
| `Installers element 0 is empty` | A slot on the scope is unassigned. Assign it or remove it. |
| You cannot tell which scope resolved what | Turn on diagnostics and open **Window > VContainer Diagnostics**. |
