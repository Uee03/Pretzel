# Pretzel Integrations

Optional adapters between Pretzel and third-party libraries. Depends on `pretzel.core`, `pretzel.tween`, `pretzel.api` and `pretzel.ui`.

Each library has its own assembly under `Runtime/`. An assembly compiles only when its Unity package is installed (`versionDefines` sets the define), so you can install any subset. Install the libraries from the Addons Extra tab or from their git URLs.

| Folder | Assembly | Library | Unity package | Define |
| --- | --- | --- | --- | --- |
| `Runtime/LitMotion` | `Pretzel.Integrations.LitMotion` | [LitMotion](https://github.com/annulusgames/LitMotion) v2.0.2 | `com.annulusgames.lit-motion` | `PRETZEL_LITMOTION` |
| `Runtime/UniTask` | `Pretzel.Integrations.UniTask` | [UniTask](https://github.com/Cysharp/UniTask) 2.5.11 | `com.cysharp.unitask` | `PRETZEL_UNITASK` |
| `Runtime/R3` | `Pretzel.Integrations.R3` | [R3](https://github.com/Cysharp/R3) 1.3.1 | `com.cysharp.r3` | `PRETZEL_R3` |

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

`ApiClient` extensions: `GetUniTaskAsync`, `PostUniTaskAsync`, `GetJsonUniTaskAsync`, `PostJsonUniTaskAsync`, `DownloadTextureUniTaskAsync` and `DownloadFileUniTaskAsync`. They take the same arguments as the `Async` methods. For other `Awaitable` values, UniTask already provides `AsUniTask()`.

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
- `ApiClient.GetAsObservable`, `GetJsonAsObservable` and `PostJsonAsObservable`.

## Sample

Import **Integrations Example** from Package Manager > Samples. It has one demo component per library and a combined one. See its README for setup.
