# Pretzel Templates

Script templates and a UI Toolkit theme template. Depends on `pretzel.core` and `pretzel.ui`, because the generated scripts use them. Editor only.

## Script templates

`Assets > Create > Pretzel`:

| Entry | Creates |
|---|---|
| C# Script | A `MonoBehaviour` with FIELDS, UNITY METHODS and METHODS regions |
| C# UI Controller | A `UIControllerBase` with `OnBind` and `OnUnbind` |
| C# Singleton | A `Singleton<T>` |
| UI Toolkit Theme | A runtime theme (`.tss`) that imports Unity's default theme and overrides it |

These replace the LazyJedi `EntityBase` and `UIControllerBase` templates. `EntityBase` is not ported, because none of its hooks were ever called.

## UI Toolkit theme template

Assign the created `.tss` to PanelSettings > Theme Style Sheet. It is organised in three parts:

1. `:root` variables: palette (`--theme-color-*`), radius, border width, spacing, font sizes and transition time.
   Change these first; the rules only read them.
2. Rules for built-in controls (`.unity-button`, `.unity-label`, text and popup inputs, toggles, sliders, progress bars),
   including hover, active, focus and disabled states.
3. Rules for Pretzel classes (`.pretzel-toast` and its modifiers) and a small BEM starter set (`.screen`,
   `.screen__title`, `.panel`, `.text--muted`) for your own screens.

For several themes (for example light and dark, or per platform), create one `.tss` per theme with different
variables and keep shared rules in a `.uss` that each theme imports with `@import url("Shared.uss");`. Switch themes at
runtime by assigning another theme to `PanelSettings.themeStyleSheet`.

## Scene templates

`Pretzel 2D` and `Pretzel 3D` ship with `pretzel.setup`. They appear in `File > New Scene` and in the Main tab of the Project Setup window.

## Samples

Each module ships its own code samples in `Samples~`, importable from the Package Manager window.
