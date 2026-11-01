# VContainer Example

Every VContainer feature in one scene with one `LifetimeScope`. Each numbered folder covers one feature: an installer
that registers it and a small demo that logs what happened. `VContainerSamplesScope` runs every installer, so reading
it top to bottom is the table of contents.

| Folder | Shows |
| --- | --- |
| `00_Shared` | The log the demos write to. `RegisterComponent` for an existing component (the `UIDocument`). |
| `01_Registration` | `Register`, `As<A, B>` (one instance, two interfaces), `WithParameter`, a registration delegate, `RegisterInstance`. |
| `02_Lifetimes` | `Singleton`, `Scoped` and `Transient`, with numbered instances; `IObjectResolver.CreateScope`. |
| `03_Injection` | Constructor, method, field and property injection; `Inject` and `InjectGameObject` for objects the container did not create. |
| `04_EntryPoints` | Every entry point interface in PlayerLoop order, `IAsyncStartable`, `UseEntryPoints` and an exception handler. |
| `05_Timers` | A tickable `TimerService`: every timer advanced from one `Tick`, pausable with one flag. |
| `06_Bullets` | A tickable `BulletSystem` moving pooled bullets from one `Tick`, fed by a spawner on a repeating timer. |
| `07_Factories` | `RegisterFactory` for a `Func<string, Enemy>`, and `resolver.Instantiate` for injected prefab instances. |
| `08_Components` | `UseComponents`, `AddInHierarchy` for a scene component, `AddOnNewGameObject` for a component created on demand. |
| `09_Collections` | Several `IReward` registrations received as `IReadOnlyList<IReward>`. |
| `10_Keyed` | `Keyed` registrations and `[Key]` parameters choosing between two `IWeapon`s. |
| `11_OpenGenerics` | `Register(typeof(Repository<>))` serving every `IRepository<T>`. |
| `12_Settings` | A serializable settings class edited on the scope, and a ScriptableObject from the scope's Settings list. |
| `13_Installers` | A `MonoInstaller` on the scope and an optional `ScriptableInstaller` asset. Every folder's installer is a plain `IInstaller`. |
| `14_Callbacks` | `RegisterBuildCallback`, `RegisterDisposeCallback` and `IDisposable` cleanup. |
| `15_ChildScopes` | Injecting the current `LifetimeScope`, `CreateChild` for three short levels, and disposing each child. |
| `16_Audio` | Optional, needs Pretzel Audio. A `MonoInstaller` registers `AudioService` as `IAudioService`; a demo asks for the interface, plays a generated tone and checks the handle later. |

## Run it

1. Install VContainer (Pretzel > Project Setup > VContainer) and import this sample.
2. In an empty scene, create a GameObject and add `VContainerSamplesScope`. It adds a `UIDocument`, a `SceneInstaller`
   and a `SceneMarker`.
3. Assign a `PanelSettings` asset to the `UIDocument` to see the log on screen; without one it goes to the Console.
4. Enter Play mode. Bullets spiral out from the origin, enemies appear at x = -2 and 2, and three levels start and end.
5. Exit Play mode to see the dispose messages in the Console.

Optional: create a Gameplay Settings asset and add it to the scope's **Settings** list, and a Scriptable Installer
asset added to its **Installers** list (both under Assets > Create > Pretzel > Samples).

Optional, with Pretzel Audio installed: add `AudioSceneInstaller` to the scope's GameObject. It registers `AudioService`
as `IAudioService` and starts `AudioDemo`, which logs a tone played through the interface. `16_Audio` has its own
assembly that compiles only when both VContainer and Pretzel Audio are installed.

Additive scenes parented with `LifetimeScope.EnqueueParent`, the diagnostics window and the source generator are
covered in the integration guide (`Documentation~/VContainer.md`), because they need a second scene or editor setup.
