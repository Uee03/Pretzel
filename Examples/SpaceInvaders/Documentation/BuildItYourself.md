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

## Editor tools

The *Space Invaders* menu is the example's own tooling. All of it is in `Assets/_Scripts/Editor`.

| Menu item | What it does |
|---|---|
| **Build Demo** | Sets the import settings of the art (`ArtImporter`), builds the sound library, creates the wave and panel assets, builds every prefab (the explosion from its eight frames, the player with `SpriteFlash` and `PunchScale`), puts the settings and the loading screen on the root scope prefab, creates both scenes from the Pretzel 2D template with a Cinemachine rig and three scrolling star layers, and sets the build settings. Every step is repeatable, and a rebuild keeps edits made to existing wave assets |
| **Rebuild Sound Library** | Reads the WAV files in `_Project/Audio/SFX` and `_Project/Audio/Music`, refreshes the cues and the `SfxLibrary`, and fills the `SoundBank` with the march notes and the three tunes (imported as streamed Vorbis) |
| **Build Windows Player** | Builds the two scenes to `Builds/SpaceInvaders` (git-ignored). Pass `-buildOutput <path>` to choose another file |

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
