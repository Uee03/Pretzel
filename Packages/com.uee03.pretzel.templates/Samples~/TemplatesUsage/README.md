# Templates Usage

Pretzel adds three script templates under **Assets > Create > Pretzel**. Each script in this sample
started from one of them and was then filled in.

| Menu | Template output | Finished example |
|------|-----------------|------------------|
| C# Script | `MonoBehaviour` with `FIELDS`, `UNITY METHODS` and `METHODS` regions | `Spinner.cs` |
| C# Singleton | `Singleton<T>` with an `Awake` that calls `base.Awake()` | `ScoreKeeper.cs` |
| C# UI Controller | `UIControllerBase` with `OnBind` and `OnUnbind` | `ScoreHudController.cs` + `ScoreHud.uxml` |

The templates use the project's root namespace (**Project Settings > Editor > Root namespace**) when it
is set.

## Try it

1. Create a cube and add `Spinner`.
2. Choose **GameObject > UI Toolkit > UI Document**, set its **Source Asset** to `ScoreHud.uxml`, and add
   `ScoreHudController` to the same object.
3. Press Play and click **Add point**. `ScoreKeeper` is created on first use by `ScoreKeeper.Instance`.

Rules the finished scripts follow:

- Register callbacks in `OnBind` and remove them in `OnUnbind`, so disabling the object leaves nothing behind.
- Call `base.Awake()` first in a singleton, so duplicates are destroyed before your code runs.
- Check a singleton with `if (_scoreKeeper)` during teardown, because it can be destroyed first.
