# Build it yourself

The order a new project would follow to end up with this game, the editor tools it ships with, and how to test and build it.

## 1. From an empty template to a composition root

1. **Create the project** from the *Universal 2D* template. This example removed Version Control and Visual Scripting from the manifest and kept every other default.
2. **Install Pretzel Setup**, the only package you add by hand. In your own project, add it as a git dependency:

   ```json
   "com.uee03.pretzel.setup": "https://github.com/Uee03/Pretzel.git?path=Packages/com.uee03.pretzel.setup#v0.2.0"
   ```

   This example lives next to the packages, so its manifest uses `file:../../../Packages/<name>` paths instead.
3. **Open Pretzel > Project Setup.** On *Main*, set the company and product name and create the *Game* folder preset. On *Addons*, install the modules you need: core, extensions, tween, data, events, pooling, scenes, ui, audio, preferences, devconsole and vcontainer, plus VContainer.
4. **VContainer tab.** Set the namespace (`SpaceInvaders`), keep *Starter services* on, and tick the Pretzel services you use: event bus, scene loader, data store, preferences, pool registry and audio. Add one custom service, `GameNavigator`, with an interface. Click **Generate and set up**.
5. After compilation the tool creates the installer asset, the scope prefab and the VContainer settings. Pressing Play now runs `GameFlow.Start` in any scene.

The same steps can run without the window, from a script or CI:

```powershell
Unity -batchmode -projectPath . -executeMethod Pretzel.Setup.PretzelSetupBatch.Composition `
      -pretzelNamespace SpaceInvaders -pretzelServices events,scenes,data,preferences,pooling,audio
```

Do not pass `-quit` to that command. It quits itself once the assets exist.

## 2. The order the game was written in

1. **Rules first, as plain C#**, with EditMode tests: `MarchLogic`, `MarchTempo`, `ScoreTable`, `RunState`, `WaveSet`. No scene is needed to run them.
2. **Messages.** One `readonly struct` per thing that happens (`Gameplay/Messages`).
3. **Actors**, each small and unaware of the others: `PlayerShip`, `Projectile`, `Invader`, `InvaderFormation`, `Ufo`, `BunkerBlock`, `ExplosionFx`. They get an `IEventBus` and `IProjectileSpawner` by method injection and publish events.
4. **Pools.** `ProjectileSpawner` and `InvaderFormation` own `PoolFactory`s; `ExplosionSpawner` uses the registry.
5. **Level scope.** `LevelInstaller` registers the scene components and the presenters. `LevelPresenter` holds the rules of a game, `HudPresenter` keeps the HUD in step, `GameAudioReactions` plays sounds, `DevCommands` registers console commands.
6. **UI.** UXML and USS first (`_Project/UI`), then `HudView` and `TitleView` as thin controllers, then their presenters.
7. **Composition.** Fill in the generated `GameFlow` and `GameNavigator`, add the loading screen to the root scope prefab and register it in `GameInstaller.cs`.
8. **Content.** Waves, the sound bank and the scenes, created by the builder (below) and then tuned by hand.
9. **PlayMode tests** that play the real scenes through the real container.

A useful rule that came out of this: if a class needs a `GameObject`, ask whether the decision it makes can live in a plain class next to it.

## 3. Adding co-op

Co-op was added last, on top of a finished single-player game, and the single-player code path was left alone. The order was:

1. **Decide the packages.** Co-op needs Netcode for GameObjects (2.13.3), Multiplayer Services (2.4.1, which brings Authentication and Core) and the Pretzel Multiplayer package. These are the versions the Multiplayer package declares and `TestPretzel` already resolves together on Unity 6000.6.5f1, so they were added to `Packages/manifest.json` and nothing else was updated.
2. **Rules first, as plain C#:** `CoopRunState` (a life count and a score for each player, and game over when everyone is out), with EditMode tests.
3. **The replicated values:** `CoopPlayerStat`, `FormationSnapshot` and `UfoSnapshot`, small `INetworkSerializable` structs, with a round-trip test through NGO's own buffer writer.
4. **Seams in the single-player actors**, so the same objects can be mirrored on a client: `InvaderFormation` can switch its simulation off, report an alive mask and apply one; `Ufo.Mirror`, `BunkerField.ReadHealths` and `BunkerBlock.SetHealth` do the same for the mystery ship and the bunkers. The single-player game never calls them.
5. **Network actors:** `CoopShip`, `CoopProjectile` and `CoopProjectileSpawner`. The formation drops bombs through the same `IProjectileSpawner` interface it uses in single player.
6. **The shared hub and the host's rules:** `CoopMatch` (replicated phase, wave and stats, and the event relay), `CoopWorldReplicator`, and `CoopMatchDirector`, which is the host-only counterpart of `LevelPresenter`.
7. **UI and composition:** `CoopHudView` and `CoopHudPresenter` with `CoopHud.uxml`, `CoopLevelInstaller`, a CO-OP button on the title and `IGameNavigator.PlayCoopAsync`.
8. **Content:** the network prefabs and the Coop scene, built by **Space Invaders > Build Co-op**.
9. **Tests that play it:** a real host on loopback, the client-side mirror logic, and a two-process test with a real remote client.

### Run it

1. Open the project and press Play from the Title scene, or open `Assets/_Project/Scenes/Coop.unity`. Choose **CO-OP**.
2. **Local co-op:** choose *Local co-op*, leave the number of local players at 2 and start the match. Player 1 uses A / D and Space, player 2 the arrow keys and Enter. Gamepads also work, one for each player.
3. **Online:** the project must be linked to a Unity Cloud project that has Authentication and Multiplayer Services (Relay) enabled (*Edit > Project Settings > Services*). Choose *Host online* on one machine and give the join code to the other, who chooses *Join with code*. To try it on one machine, open a second copy of the project (or a built player) and give each copy a different **Authentication Profile** on `Network Manager > PretzelNetworkBootstrap > Settings`, so that they sign in as two different players.
4. The link to your Unity Cloud project is stored in `ProjectSettings/ProjectSettings.asset`. A git filter blanks it when you commit; see the Multiplayer notes in [Where Pretzel is used](PretzelUsage.md#multiplayer).

## Editor tools

The *Space Invaders* menu is the example's own tooling. All of it is in `Assets/_Scripts/Editor`.

| Menu item | What it does |
|---|---|
| **Build Demo** | Sets the import settings of the art (`ArtImporter`), builds the sound library, creates the wave and panel assets, builds every prefab (the explosion from its eight frames, the player with `SpriteFlash` and `PunchScale`), puts the settings and the loading screen on the root scope prefab, creates the Title, Game and Coop scenes from the Pretzel 2D template with a Cinemachine rig and three scrolling star layers, and sets the build settings. Every step is repeatable, and a rebuild keeps edits made to existing wave assets |
| **Build Co-op** | Builds only the co-op content: the ship, bullet and bomb network prefabs (`ActorPrefabsBuilder.Coop.cs`), the network prefab list, and the Coop scene with its `NetworkManager`, the Pretzel menu, the HUD and the scope (`SceneBuilder.Coop.cs`). Run it after changing a co-op prefab recipe. It is also the batch entry point: `-executeMethod SpaceInvaders.Editor.DemoBuilder.BuildCoop` |
| **Rebuild Sound Library** | Reads the WAV files in `_Project/Audio/SFX` and `_Project/Audio/Music`, refreshes the cues and the `SfxLibrary`, and fills the `SoundBank` with the march notes and the three tunes (imported as streamed Vorbis) |
| **Build Windows Player** | Builds the three scenes to `Builds/SpaceInvaders` (git-ignored). Pass `-buildOutput <path>` to choose another file |

The committed scenes and assets came from **Build Demo**, so you do not need to run it. It is there so you can see how generated content is kept repeatable, and to rebuild after changing a prefab recipe.

`WaveDefinitionEditor` customises the Inspector of the wave assets (`_Project/ScriptableObjects/Waves`): it adds a live preview of the formation and warns when a wave has no rows, a tempo that contradicts itself, or a formation wider than the playfield.

Two rules in the builder code are worth copying: set private `[SerializeField]` values through `SerializedObject` rather than adding setters, and reload assets by path after opening a scene, because Unity can unload objects that were created earlier in the same run.

## Tests

Run them from the Test Runner window, or from the command line. Run the two suites one after the other, and read each result file before starting the next job: Unity clears the project's `Temp` folder when it starts.

```powershell
$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe'
$Project = 'Examples\SpaceInvaders'

& $Unity -batchmode -projectPath $Project -runTests -testPlatform EditMode `
         -assemblyNames SpaceInvaders.Tests.EditMode -testResults editmode.xml -logFile editmode.log
& $Unity -batchmode -projectPath $Project -runTests -testPlatform PlayMode `
         -assemblyNames SpaceInvaders.Tests.PlayMode -testResults playmode.xml -logFile playmode.log
```

Do not add `-quit` to a test run, and do not add `-nographics` to the PlayMode run, which takes screenshots.

| Suite | What it proves |
|---|---|
| `Tests/EditMode` | The march turns and drops at the edges; the tempo only ever gets faster; scores; the wave lap rules; lives and waves. `GameFeelReactionsTests` and `GameAudioReactionsTests` check which sound, freeze, shake and backdrop speed each event asks for, against fakes (`Fakes.cs`) |
| `Tests/PlayMode/FeelAndInputTests.cs` | In the real scenes: a hit gives a hit stop, a shake and a flash and time returns to 1; a pause is never undone by an overlapping effect; the playfield stays fully visible at 16:9, 1:1 and 9:16; the star layers scroll by themselves and stop on pause; pausing changes the input context; input is blocked during a scene load |
| `Tests/PlayMode/GameFlowTests.cs` | The title boots the root scope and loads progress. A full game: a real bullet kills an invader through pooling and physics, bombs drop, a cleared wave starts the next, a hit makes the ship invulnerable, three hits end the game and save the high score, and the game returns to the title. Pause stops time. The console commands change the game |
| `Tests/EditMode/CoopRunStateTests.cs`, `CoopSnapshotTests.cs` | The co-op rules: separate lives and scores, game over only when every player is out, ties, rejoining keeps the score. The replicated values survive NGO's wire format exactly and compare equal only when nothing changed |
| `Tests/PlayMode/CoopFlowTests.cs` | A real NGO host on loopback with two local players, playing the real Coop scene: a ship for each player, the first wave, a bullet that scores for the player who fired it and returns to its pool, a bomb that costs only the ship it hits, one player out without ending the match, and back to the lobby for a fresh match |
| `Tests/PlayMode/CoopMirrorTests.cs` | What a client does with the host's snapshots, on the single-player scene's real actors: a mirrored formation follows the host's block, frame and survivors without marching, and the mystery ship and bunkers show what the host reports |
| `Tests/PlayMode/CoopRemoteHostTests.cs`, `CoopRemoteClientTests.cs` | The two-process test with a real remote client (below). Marked `[Explicit]`, so a normal run skips them |

### The two-process co-op test

One Unity project can only be open once, so the host and the client run in two copies of the project. The host test starts a local session (it listens on `127.0.0.1:7777`), waits for the client, starts the match and checks that the remote player scored. The client test joins as a genuine remote client, checks that it built the host's full wave without marching, controls one of the two ships, fires, and checks that the kill came back as replicated state, relayed events and a score. They coordinate through marker files in `%TEMP%\SpaceInvadersRemoteCoop`.

```powershell
# Copy the project once (Library included, so the second copy starts fast), then start the host first.
robocopy Examples\SpaceInvaders Examples\SpaceInvadersClient /E /XD Temp Logs Builds
$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe'
Start-Process $Unity -ArgumentList '-batchmode','-projectPath','Examples\SpaceInvaders','-runTests','-testPlatform','PlayMode','-testFilter','CoopRemoteHostTests','-testResults','host.xml','-logFile','host.log'
Start-Process $Unity -ArgumentList '-batchmode','-projectPath','Examples\SpaceInvadersClient','-runTests','-testPlatform','PlayMode','-testFilter','CoopRemoteClientTests','-testResults','client.xml','-logFile','client.log'
```

Close the Editor first, or run the host from a second copy too. By default the test connects over loopback: it needs no Unity Cloud project, and it covers everything the game adds on top of the connection (replication, the event relay, ownership and the host's validation).

To go through Relay instead, set `SI_REMOTE_ONLINE=1` in the environment of both processes. The host then calls `HostOnlineAsync` and passes the join code through the marker file, and the client calls `JoinOnlineAsync` by code and presses Ready. Each process uses its own Authentication Profile, so they sign in as two players. This needs the project linked to a Unity Cloud project with Authentication and Multiplayer Services (Relay) enabled, and network access. Set `SI_SCREENSHOT_DIR` to a folder to keep the screenshots that the PlayMode tests take; Unity deletes the project's `Temp` folder when it exits.

The PlayMode tests save screenshots to `Temp/TestScreenshots`. `ScreenshotUtility` renders the camera and every UI panel to render textures and blends them, because `WaitForEndOfFrame` never completes in batch mode. Copy the pictures out before the next Unity run.

## Building a player

Use **Space Invaders > Build Windows Player**, or the same method from the command line:

```powershell
& $Unity -batchmode -quit -projectPath $Project -executeMethod SpaceInvaders.Editor.PlayerBuilder.BuildWindows
```

The result opens on the title screen. A build touches URP's prefilter settings, so expect `Assets/Settings/UniversalRP.asset` to show as modified afterwards.

## Things to try next

- Add a wave: duplicate a wave asset, edit it in the Inspector and add it to the `WaveSet`.
- Add a new bomb kind: a new prefab, a `ProjectileKind` value and an entry in the `ProjectileSpawner` factory. Nothing else changes.
- Add a console command in `DevCommands`.
- Replace the sounds: drop a WAV with the same name into `_Project/Audio/SFX` and run **Rebuild Sound Library**, or edit `Art~/*.js` and run `Art~/build-audio.ps1` (SoundCraft). The same goes for the art: `Art~/build.ps1` runs the Aseprite scripts.
- Tune the feel: every number is a constant at the top of `Feel/GameFeelReactions.cs`.
