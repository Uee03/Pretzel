# Pretzel SFX

Sound effects by name, on top of [Pretzel Audio](../com.uee03.pretzel.audio/README.md). Game code says *what* happened (`"pickup_coin"`); the library says which sound that is, how often it may play and how many voices it may use. It also ships a pack of thirteen generated game sounds.

| Piece | What it does |
|---|---|
| `SfxLibrary` | An asset of ids to `AudioCue`s, edited in the Inspector |
| `SfxEntry` | One sound: id, cue, cooldown, and a limit on simultaneous voices |
| `SfxPlayer` | Plays a sound by id through `IAudioService`, skipping it when it is too frequent or its voices are all busy |
| `ISfxPlayer` | What game code depends on, so a test can pass a fake |
| `SfxThrottle` | The cooldown logic as plain C# |

```csharp
// Create the library as an asset: Create > Pretzel > Audio > SFX Library, then add entries.
[SerializeField] private SfxLibrary _library;

ISfxPlayer sfx = new SfxPlayer(audioService, _library);

sfx.Play("pickup_coin");
sfx.Play("explosion", transform.position);
sfx.Stop("stinger_win");
```

## Why a library

- **No string cues scattered through the code.** Rename or swap a sound in one asset and every call site follows. A designer changes the clip, the pitch range or the volume without touching code.
- **Machine-gun protection.** A sound fired every frame (footsteps, a laser, a coin shower) becomes a buzz. *Cooldown Seconds* drops calls that come too soon, and *Max Concurrent* drops calls while that many voices of the sound still play. A skipped call returns `AudioHandle.None` and counts in `SfxPlayer.Suppressed`.
- **Variation lives in the cue.** Give an `AudioCue` several clips and a pitch range, and repeats of one sound stay lively. The player does not duplicate that.
- **Mistakes are visible.** An id that is not in the library, or a cue without clips, logs one warning and then stays quiet.

Cooldowns use unscaled time by default, so hit stop does not block sounds. Pass your own clock to the `SfxPlayer` constructor to change that.

## Ids

Ids are case-sensitive strings. Keep them in one place:

```csharp
public static class Sounds
{
    public const string Coin = "pickup_coin";
    public const string Boom = "explosion";
}
```

## With and without a container

`SfxPlayer` takes its two dependencies in the constructor. With VContainer register it as `ISfxPlayer`; without a container create it in the same bootstrap that creates the rest of your services (see [Without a container](../com.uee03.pretzel.setup/Documentation~/WithoutAContainer.md)).

## The sound pack

**SFX Pack** (Package Manager > Samples) contains thirteen sounds: `ui_click`, `ui_confirm`, `ui_back`, `ui_error`, `pickup_coin`, `power_up`, `jump`, `laser`, `hit_light`, `hit_heavy`, `explosion`, `stinger_win` and `stinger_lose`. Each has an `AudioCue` with a pitch range, and the library gives each a cooldown and voice limit. The scene `SfxBoard` has a button per sound, and a *Mash* toggle that calls one sound every frame so you can watch the throttle skip calls.

The sounds were made with SoundCraft: the generator and the session sources are in `Audio~`, and `build.ps1` rebuilds the WAV files (16-bit, 44.1 kHz, normalized to -0.1 dBFS). They are synthesized placeholders, checked for level, clipping and clean tails, but they were **not listened to**: audition them and replace any that do not suit the game. **Pretzel > Samples > SFX > Build Library And Scene** rebuilds the cues, the library and the scene.
