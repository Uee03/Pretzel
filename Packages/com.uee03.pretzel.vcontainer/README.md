# Pretzel VContainer

[VContainer](https://github.com/hadashiA/VContainer) scopes for Pretzel, in a package of their own. It needs no other Pretzel module, so a project can install just this and, for example, Pretzel Data.

The assembly compiles only when VContainer (`jp.hadashikick.vcontainer`) is installed. Install VContainer from **Pretzel > Project Setup > VContainer** or from its git URL.

| Type | What it does |
|---|---|
| `PretzelLifetimeScope` | A `LifetimeScope` configured in the Inspector: settings assets, installer assets, and the scene installers on the same object |
| `ScriptableInstaller` | Registrations kept in an asset, shared by every scene |
| `MonoInstaller` | Registrations that need references to objects in one scene |

```csharp
using Pretzel.Integrations.VContainer;
using VContainer;
using VContainer.Unity;

public sealed class GameInstaller : ScriptableInstaller
{
    public override void Install(IContainerBuilder builder)
    {
        builder.Register<ISaveService, SaveService>(Lifetime.Singleton);
        builder.RegisterEntryPoint<GameFlow>();
    }
}
```

The assembly is named `Pretzel.Integrations.VContainer`, and the types live in the `Pretzel.Integrations.VContainer` namespace. They were part of `com.uee03.pretzel.integrations` until they moved here, and keeping the names means existing code, assembly references and prefabs work unchanged once this package is installed.

## Read next

- [Documentation~/VContainer.md](Documentation~/VContainer.md): whether you need a container, setup, scopes and lifetimes, MonoBehaviours, scene loading, testing, and practices to avoid.
- [Without a container](../com.uee03.pretzel.setup/Documentation~/WithoutAContainer.md): how to share services with ScriptableObjects or a plain bootstrap, using the Pretzel modules.
- **Pretzel > Project Setup > VContainer** generates the installer, a flow class, the services you pick and the root scope prefab. The same step runs from the command line (see the Setup README).

## Sample

Import **VContainer Example** from Package Manager > Samples: every VContainer feature in one scene with one `LifetimeScope`, one folder per feature.
