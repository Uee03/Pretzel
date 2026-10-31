# Pretzel Integrations

Optional adapters between Pretzel and third-party libraries. Depends on `pretzel.core`, `pretzel.tween`, `pretzel.api` and `pretzel.ui`.

Each library has its own assembly under `Runtime/`. An assembly compiles only when its Unity package is installed (`versionDefines` sets the define), so you can install any subset. Install the libraries from the Addons Extra tab or from their git URLs.

| Folder | Assembly | Library | Unity package | Define |
| --- | --- | --- | --- | --- |
| `Runtime/LitMotion` | `Pretzel.Integrations.LitMotion` | [LitMotion](https://github.com/annulusgames/LitMotion) v2.0.2 | `com.annulusgames.lit-motion` | `PRETZEL_LITMOTION` |
| `Runtime/UniTask` | `Pretzel.Integrations.UniTask` | [UniTask](https://github.com/Cysharp/UniTask) 2.5.11 | `com.cysharp.unitask` | `PRETZEL_UNITASK` |
| `Runtime/R3` | `Pretzel.Integrations.R3` | [R3](https://github.com/Cysharp/R3) 1.3.1 | `com.cysharp.r3` | `PRETZEL_R3` |
| `Runtime/R3Events` | `Pretzel.Integrations.R3.Events` | R3 and `pretzel.events` | `com.cysharp.r3` and `com.uee03.pretzel.events` | `PRETZEL_R3` and `PRETZEL_EVENTS` |
| `Runtime/VContainer` | `Pretzel.Integrations.VContainer` | [VContainer](https://github.com/hadashiA/VContainer) 1.19.0 | `jp.hadashikick.vcontainer` | `PRETZEL_VCONTAINER` |

## VContainer

A composition root for your game: `PretzelLifetimeScope` (configured in the Inspector), `ScriptableInstaller` for
project-wide registrations and `MonoInstaller` for scene objects. **Pretzel > Project Setup > VContainer** installs
VContainer, generates the installer, services and flow class, and creates a root scope that VContainer starts before
the first scene. Read the guide in [Documentation~/VContainer.md](Documentation~/VContainer.md): setup, scopes and
lifetimes, MonoBehaviours, scene loading, testing, and the practices to avoid.

## LitMotion

```csharp
using LitMotion;
using Pretzel.Integrations.LitMotion;
using Pretzel.Tween;

LMotion.Create(0f, 1f, 0.5f)
    .WithEase(EaseType.OutBack)
    .BindToLocalPositionY(transform);

Ease ease = EaseType.InOutCubic.ToLitMotionEase();
```

- `EaseType.ToLitMotionEase()`: every `EaseType` maps to the `Ease` of the same name.
- `MotionBuilder.WithEase(EaseType)`: sets the ease on any builder.

## UniTask

```csharp
using Cysharp.Threading.Tasks;
using Pretzel.Integrations.UniTask;

Response<Item> item = await client.GetJsonUniTaskAsync<Item>("items/1", cancellationToken: token);
```

`ApiClient` extensions: `GetUniTaskAsync`, `PostUniTaskAsync`, `GetJsonUniTaskAsync`, `PostJsonUniTaskAsync`, `DownloadTextureUniTaskAsync` and `DownloadFileUniTaskAsync`. They extend `IApiClient`, so they work on a fake client in tests too, and take the same arguments as the `Async` methods. For other `Awaitable` values, UniTask already provides `AsUniTask()`.

## R3

R3 also needs its NuGet package, installed with NuGetForUnity, before the Unity package compiles. The Addons Extra tab guides you through both.

```csharp
using Pretzel.Integrations.R3;
using R3;

// UI Toolkit
button.ClickedAsObservable().Subscribe(_ => Debug.Log("clicked")).AddTo(this);
panel.EventAsObservable<PointerEnterEvent>().Subscribe(_ => Highlight()).AddTo(this);

// ApiClient: each subscription sends one request. Disposing it cancels the request.
client.GetJsonAsObservable<Item>("items/1").Subscribe(response => { });
```

- `VisualElement.EventAsObservable<TEvent>()` and `Button.ClickedAsObservable()` unregister their callbacks when disposed.
- `IApiClient.GetAsObservable`, `GetJsonAsObservable` and `PostJsonAsObservable`.

### R3 and Pretzel Events

An optional second assembly, `Pretzel.Integrations.R3.Events`, compiles only when both R3 and `pretzel.events` are installed.
It turns the bus and the ScriptableObject channels into R3 streams, and streams back into events:

```csharp
using Pretzel.Integrations.R3;
using R3;

// EventBus
bus.AsObservable<EnemyDefeated>()
    .Scan(0, (total, e) => total + e.Points)
    .Subscribe(total => scoreLabel.text = "Score: " + total)
    .AddTo(this);

// Channels (assets): the payload stream, and a void channel that emits Unit
intChannel.AsObservable().Where(hp => hp < 20).Subscribe(_ => ShowWarning()).AddTo(this);
quitChannel.AsObservable().Subscribe(_ => Quit()).AddTo(this);

// A stream publishes on the bus, or raises a channel
button.ClickedAsObservable().Select(_ => new MenuOpened()).PublishTo(bus).AddTo(this);
damage.RaiseOn(damageChannel).AddTo(this);
```

- `IEventBus.AsObservable<T>()`, `EventChannel<T>.AsObservable()` and `VoidEventChannel.AsObservable()`: subscribing to the stream subscribes to the source, disposing unsubscribes.
- `Observable<T>.PublishTo(IEventBus)` and `Observable<T>.RaiseOn(EventChannel<T>)` (`Observable<Unit>.RaiseOn(VoidEventChannel)`): the reverse direction.
- Use it for what R3 is good at: operators (`Where`, `Select`, `Scan`, `Debounce`, `CombineLatest`), time and frame control, and lifetime through `AddTo`. The bus itself stays allocation-free for plain subscribers; each R3 operator in a chain allocates when the chain is built and when it emits through closures, so keep hot per-frame paths on `Subscribe<T>` directly.
- Both sources are main-thread only, so the streams emit on the main thread.

## Sample

Import **Integrations Example** from Package Manager > Samples. It has one demo component per library and a combined one. See its README for setup.
