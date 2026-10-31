# Two Cubes

Two networked cubes that can only move, jump and turn, plus **Spawn Item** and **Delete Items** buttons that spawn
server-owned items through the pooled spawn handler. It runs in local co-op, online host / join and quick join through
the drop-in menu.

| File | What it shows |
|------|---------------|
| `Editor/TwoCubesSceneCreator.cs` | Builds the prefabs, the scene, the NetworkManager (with `PretzelNetworkBootstrap`), the menu and the HUD with supported Editor APIs, and adds the scene to the build settings. |
| `Runtime/TwoCubesPlayer.cs` | An owner-authoritative player. Only the controlling device reads input and moves the object, `NetworkTransform` (authority mode Owner, interpolation on) replicates it. The colour follows the slot the host assigned through `NetworkPlayerIdentity`. |
| `Runtime/TwoCubesMotor.cs` | The movement rules (move, jump, turn) as plain C# without Unity components or networking. |
| `Runtime/ITwoCubesInput.cs`, `TwoCubesKeyboardInput.cs` | Where input comes from. The keyboard implementation uses the Input System when it is active, otherwise the legacy Input Manager. Replace it for gamepads. |
| `Runtime/TwoCubesItem.cs` | An `IPoolable` network object. The server picks its colour on every spawn (pooling does not reset `NetworkVariable` values) and local state is reset in `OnTakenFromPool`. |
| `Runtime/TwoCubesItemSpawner.cs` | Any client asks the server by Rpc to spawn an item or delete all items. The server enforces the cap (30 by default) and returns items to the pool. |
| `Runtime/TwoCubesHud.cs`, `.uxml`, `.uss` | The Spawn Item / Delete Items bar and the item counter, shown while the match runs. |

## Import and create the scene

1. Package Manager > Pretzel Multiplayer > Samples > **Two Cubes** > Import. The sample references the Input System
   assembly, so install `com.unity.inputsystem` first (a Unity 6 project has it by default).
2. `Pretzel > Samples > Multiplayer > Create Two Cubes Scene` for the 3D version, or `Create Two Cubes Scene (2D)` for
   the side-view version.
3. The creator writes the prefabs, the network prefab list, the panel settings and the scene under
   `Assets/PretzelMultiplayerSample/`, then adds the scene to the build settings. Open the scene and press Play.

For scripted use, `TwoCubesSceneCreator.CreateScene3DBatch` and `CreateScene2DBatch` are public static entry points for
`-executeMethod`.

## Controls

| Local player | Move | Jump |
|--------------|------|------|
| 1 | W A S D | Space |
| 2 | Arrow keys | Right Ctrl or Enter |

Further local players have no keys. In the 3D scene the cube moves on the ground plane and faces its movement
direction. In the 2D scene it moves left and right and turns around when the direction flips. Any player, host or
client, can press **Spawn Item** and **Delete Items**.

## Try it

- **Local**: press Play, choose **Local co-op**, leave Local players at 2 and press **Start match**. Two cubes appear,
  each with its own colour. Player 1 uses WASD, player 2 the arrow keys. Spawn a few items, then **Delete Items**: the
  counter returns to 0 and the items go back to the pool instead of being destroyed.
- **Online host**: choose **Host online**, create the session and note the join code in the lobby. This needs the
  project linked to a Unity Cloud project with Authentication and Multiplayer Services (Relay) enabled.
- **Second player**: build the scene, or use a second editor instance, and run a second copy. To run two copies on one
  machine, give each a different `Authentication Profile` in `NetworkSettings` on `PretzelNetworkBootstrap` (otherwise
  both sign in as the same player). On the second copy choose **Join with code** and enter the code, pick the session from
  the list, or use **Quick join**. The host presses **Start match** after the guest is ready.
- **Rejoin**: close the second copy while the match runs and start it again within the grace window. It offers
  **Rejoin match** and the cube resumes where it was.

Online hosting has been run against a live project; joining from a second copy has not. See the package README, "Not verified".
