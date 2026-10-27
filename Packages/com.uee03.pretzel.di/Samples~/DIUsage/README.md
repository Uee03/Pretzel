# DI Usage

| File | What it shows |
|------|---------------|
| `GameLifetimeScope.cs` | A `LifetimeScope` subclass as the composition root: a singleton service and an entry point. Instantiates a prefab with injection. |
| `ScoreService.cs` | A plain C# service constructed by the container. |
| `PassiveIncome.cs` | `IStartable` and `ITickable` entry point with constructor injection. |
| `ScoreView.cs` | A MonoBehaviour with an `[Inject]` method. |

## Try it

1. Create an empty GameObject and add `GameLifetimeScope`.
2. Add `ScoreView` to the same GameObject or a child. The scope injects its own hierarchy.
3. Press Play. The score rises by one every second and `ScoreView` logs it.
4. Optional: make a prefab with `ScoreView`, assign it to **View Prefab**, and see a second view injected at runtime.

Rules:

- Prefer constructor injection for plain C# classes; use `[Inject]` members only for MonoBehaviours.
- Resolve in the composition root, not from deep inside gameplay code. Passing `IObjectResolver` around hides dependencies.
- Unity objects cannot be constructed by the container. Register them with `RegisterInstance` or `RegisterFactory`.
- `[Inject]` derives from `PreserveAttribute`, so IL2CPP stripping keeps injected members. Constructors of registered
  types are preserved through the attribute or by being referenced; add `[Preserve]` or a `link.xml` entry if a
  High stripping level removes one.
- A child scope (set **Parent** on another `LifetimeScope`) sees its parent's services and is disposed with it.
