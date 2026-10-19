# Pretzel Templates

Script templates. Depends on `pretzel.core` and `pretzel.ui`, because the generated scripts use them. Editor only.

## Script templates

`Assets > Create > Pretzel`:

| Entry | Creates |
|---|---|
| C# Script | A `MonoBehaviour` with FIELDS, UNITY METHODS and METHODS regions |
| C# UI Controller | A `UIControllerBase` with `OnBind` and `OnUnbind` |
| C# Singleton | A `Singleton<T>` |

These replace the LazyJedi `EntityBase` and `UIControllerBase` templates. `EntityBase` is not ported, because none of its hooks were ever called.

## Scene templates

`Pretzel 2D` and `Pretzel 3D` ship with `pretzel.setup`. They appear in `File > New Scene` and in the Main tab of the Project Setup window.

## Samples

Each module ships its own code samples in `Samples~`, importable from the Package Manager window.
