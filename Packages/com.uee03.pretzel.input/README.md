# Pretzel Input

Tools on the [Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/index.html) for 2D, 2.5D and 3D games: **which actions are live right now**, input that can be switched off for a reason, buffered presses, device tracking, and rebinding that survives a restart. The package depends on `com.unity.inputsystem` and on nothing from Pretzel.

| Piece | What it does |
|---|---|
| `InputContextStack` | The contexts of the game as a stack: Gameplay, a Menu above it, a Dialog above that |
| `InputBlockers` | Reasons why input is off (a scene load, a cutscene). Input is blocked while any reason is held |
| `InputRouter` | Enables the action map of the current context, disables the rest, and disables everything while blocked |
| `InputBuffer` | Remembers a press for a short window, for jump buffering and the like |
| `InputDeviceTracker` | Which kind of device the player used last, so prompts can follow it |
| `RebindSession` | One safe interactive rebind: disables the action while waiting, Escape cancels, always disposes |
| `BindingPersistence`, `IBindingStore` | Load, save and reset the rebinds through a store |

## Contexts

Name your action maps after your contexts (`Gameplay`, `Menu`, `Dialog`), give the router the asset, and push and pop contexts from game code:

```csharp
InputRouter router = new(actionsAsset);
router.Contexts.Push("Gameplay");

pause.performed += _ => router.Contexts.Push("Menu");      // Gameplay stops, Menu starts
resume.performed += _ => router.Contexts.Remove("Menu");   // Gameplay is back
```

The router enables the current context's map and disables every other, so there is never an action firing in the wrong place and no `if (paused)` checks in gameplay code. Pushing a context that is already on the stack moves it to the top; removing one from the middle leaves the top alone. An optional *always on* map (for a debug console) stays enabled in every context.

## Blocking input

A scene load, a cutscene or a transition should not receive input. Block it for a **reason**:

```csharp
router.Blockers.Block("cutscene");
router.Blockers.Unblock("cutscene");
```

Every map is off while any reason is held. Reasons are names, so blocking twice for the same reason counts once, and releasing a reason that is not held does nothing: input can neither get stuck nor come back too early. Contexts are untouched, so the right map returns when the last reason is released.

With Pretzel Scenes installed, `SceneLoadInputGate` does this for every scene load. Pretzel Scenes raises `LoadFinished` after every load, successful or not, so a failed load cannot leave input blocked:

```csharp
using SceneLoadInputGate gate = new(sceneLoader, router);
```

## Buffering

```csharp
InputBuffer jump = new(windowSeconds: 0.15f);

jumpAction.performed += _ => jump.Press(Time.time);

if (isGrounded && jump.Consume(Time.time))
{
    Jump();
}
```

A jump pressed 0.1 seconds before landing still jumps on landing, and one press counts once.

## Devices and prompts

```csharp
InputDeviceTracker devices = new();
devices.Changed += kind => RefreshButtonPrompts(kind);   // KeyboardMouse, Gamepad or Touch
```

It listens to the actions that fire, so it needs no polling, and it raises an event only when the kind changes. Mouse and keyboard are one kind.

## Rebinding

```csharp
RebindSession session = RebindSession.Start(jumpAction, bindingIndex: 0, result =>
{
    if (result == RebindResult.Completed)
    {
        BindingPersistence.Save(actionsAsset, store);
    }
});
```

The session disables the action while it waits (the Input System requires that) and enables it again afterwards, ignores mouse movement, lets Escape cancel, and disposes the underlying operation on every path, including when you dispose the session early (no callback is made then).

Rebinds are stored by an `IBindingStore`. Apply them once at startup, and keep a copy of the asset if you do not want rebinding to touch the asset on disk:

```csharp
BindingPersistence.Apply(actionsAsset, store);   // startup
BindingPersistence.Save(actionsAsset, store);    // after a rebind
BindingPersistence.Reset(actionsAsset, store);   // restore the defaults
```

`PlayerPrefsBindingStore` works anywhere. With Pretzel Preferences installed, use `PreferencesBindingStore`, which keeps the rebinds in the player's control settings, so they save and reset with the rest of the preferences.

## Local multiplayer

One router per player, each with its own copy of the asset (`Instantiate(actions)`) and its own devices through `InputUser` or `PlayerInput`, gives every player their own contexts and blockers. The router does not assume one player.

## Optional adapters

`Pretzel.Inputs.Preferences` and `Pretzel.Inputs.Scenes` compile only when Pretzel Preferences or Pretzel Scenes is installed, so the package never pulls them in.

## Sample

**Input Example** (Package Manager > Samples) is a square you move and jump with, a pause that switches contexts and stops the square, a buffered jump, a live context and device readout, a rebind button with saved overrides, and a button that blocks input for two seconds the way a scene load does. **Pretzel > Samples > Input > Build Scene And Assets** rebuilds the scene and the actions asset.
