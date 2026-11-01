# SFX Pack

Thirteen game sounds, their audio cues, an `SfxLibrary` that gives each a cooldown and a voice limit, and a sound board.

Open `Scenes/SfxBoard` and press Play. Click a button to play a sound. Turn on **Mash** to call `hit_light` every frame and watch the counters: calls inside the 0.08 s cooldown, or beyond 3 simultaneous voices, are skipped.

| Sound | Channel | Cooldown | Voices |
|---|---|---|---|
| `ui_click` `ui_confirm` `ui_back` `ui_error` | UI | 0.03 to 0.15 s | no limit |
| `pickup_coin` | Sfx | 0.05 s | 4 |
| `power_up` | Sfx | none | 1 |
| `jump` | Sfx | 0.1 s | no limit |
| `laser` | Sfx | 0.07 s | 4 |
| `hit_light` | Sfx | 0.08 s | 3 |
| `hit_heavy` `explosion` | Sfx | none | 2 |
| `stinger_win` `stinger_lose` | Sfx | none | 1 |

| File | Role |
|---|---|
| `Audio/*.wav` | The sounds, made with SoundCraft (the sources are in the package's `Audio~` folder). Synthesized and **not listened to**: audition them before shipping |
| `Cues/*.asset`, `SfxLibrary.asset` | One cue per sound, with a small pitch range, and the library that names them |
| `Runtime/SfxBoard.cs` | The UI Toolkit board that calls `SfxPlayer` |
| `Editor/SfxPackBuilder.cs` | Builds the cues, the library and the scene. *Pretzel > Samples > SFX > Build Library And Scene* |
