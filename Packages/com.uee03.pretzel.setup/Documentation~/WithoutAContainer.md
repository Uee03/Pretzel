# Without a container

Pretzel does not need a dependency injection container. The Setup tool can generate a VContainer composition root, and the [Pretzel VContainer](../../com.uee03.pretzel.vcontainer/README.md) module has the scopes, but every other module is a plain class or component you can create yourself. If you already share services and managers between objects and scenes with ScriptableObjects, you can keep doing that and use Pretzel the same way.

This page shows one way to do it. The pieces are small, so change them to fit your game. The **No Container** sample (Package Manager > Pretzel Setup > Samples) is a complete, tested game built this way: a services asset, a bootstrap, UI Toolkit screens, saving and preferences.

## How each module is created

| Module | Without a container |
|---|---|
| Events | `new EventBus()`, or `VoidEventChannel` and the typed channel assets, which are ScriptableObjects already |
| Pooling | `new PoolRegistry()` |
| Scenes | `new SceneLoader()` |
| Data | `DataIO.Default`, or your own `IDataStore` |
| Preferences | `new PreferencesService()` (uses `DataIO.Default`), or pass an `IDataStore` |
| Audio | `AudioService` is a component: keep it on a prefab and instantiate it once |
| UI, DevConsole, Tween | Components and static helpers. Nothing to wire |

Each one takes its dependencies in the constructor, with defaults, so none of them reach for a global.

## A services asset and a bootstrap

One ScriptableObject holds the services, and a bootstrap method fills it before the first scene loads. Objects get the asset through a serialized field.

`GameServices.cs`

```csharp
using Pretzel.Audio;
using Pretzel.Events;
using Pretzel.Pooling;
using Pretzel.Preferences;
using Pretzel.Scenes;
using UnityEngine;

namespace MyGame
{
    [CreateAssetMenu(menuName = "My Game/Services")]
    public sealed class GameServices : ScriptableObject
    {
        [SerializeField] private AudioService _audioPrefab;

        public IEventBus Events { get; private set; }
        public SceneLoader Scenes { get; private set; }
        public IPoolRegistry Pools { get; private set; }
        public IPreferencesService Preferences { get; private set; }
        public IAudioService Audio { get; private set; }

        public void Initialize()
        {
            Events = new EventBus();
            Scenes = new SceneLoader();
            Pools = new PoolRegistry();
            Preferences = new PreferencesService();

            AudioService audio = Instantiate(_audioPrefab);
            DontDestroyOnLoad(audio.gameObject);
            Audio = audio;
        }
    }
}
```

`GameBootstrap.cs`

```csharp
using UnityEngine;

namespace MyGame
{
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Start()
        {
            GameServices services = Resources.Load<GameServices>("GameServices");
            services.Initialize();
        }
    }
}
```

Put the asset at `Resources/GameServices.asset`. The method runs once per Play session, before any scene's `Awake`, from any start scene. A scene object then uses it like this:

```csharp
using System;
using UnityEngine;

namespace MyGame
{
    public sealed class ScoreLabel : MonoBehaviour
    {
        [SerializeField] private GameServices _services;

        private IDisposable _subscription;

        private void OnEnable()
        {
            _subscription = _services.Events.Subscribe<ScoreChanged>(OnScoreChanged);
        }

        private void OnDisable()
        {
            _subscription?.Dispose();
        }

        private void OnScoreChanged(ScoreChanged message)
        {
            Debug.Log($"Score: {message.Score}");
        }
    }
}
```

```csharp
namespace MyGame
{
    public sealed class ScoreChanged
    {
        public ScoreChanged(int score)
        {
            Score = score;
        }

        public int Score { get; }
    }
}
```

Anything can raise it with `_services.Events.Publish(new ScoreChanged(120));`.

## Keeping it reliable

Runtime state in an asset behaves differently from state in a plain object. These rules avoid the usual surprises.

- **Create the services in `Initialize`, never in a field initializer or `OnEnable`.** With *Enter Play Mode Options* and domain reload off, the asset keeps the previous session's objects. `Initialize` runs at the start of every session and replaces them. Dispose anything that needs it first if you call `Initialize` more than once.
- **Keep configuration serialized and runtime state in properties without a public setter.** Serialized fields are saved in the asset. Auto-properties are not, so a build starts empty and the bootstrap fills them.
- **Fill the asset in one place.** A single bootstrap gives you one order of creation. If one service needs another, create them in that order in `Initialize`.
- **Depend on interfaces where you want fakes.** The properties above are interfaces (`IEventBus`, `IPoolRegistry`, `IAudioService`, `IPreferencesService`) for that reason. `SceneLoader` is a concrete class.
- **Use event channels for designer wiring.** A `VoidEventChannel` asset dropped into two components needs no service at all.
- **Test with a fresh asset.** `ScriptableObject.CreateInstance<GameServices>()` and call the parts you need, or give the class under test just the `IEventBus` it uses.

## Choosing

A container and a services asset solve the same problem, so pick the one that suits the team.

| | Services asset | Container |
|---|---|---|
| Learning curve | Unity concepts only | Scopes, lifetimes, injection |
| Wiring | Drag the asset into fields | Registrations in code |
| Dependencies visible | In the serialized fields | In every constructor |
| Cleanup | Yours, in `Initialize` and on quit | The container disposes what it created |

You can also mix them: keep designer-owned configuration in ScriptableObjects and register those assets as settings of a `PretzelLifetimeScope`. Moving from one to the other later changes how objects get their services, not the services themselves.
