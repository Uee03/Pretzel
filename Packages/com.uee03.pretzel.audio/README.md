# Pretzel Audio

An audio service component with pooled voices, cue assets, channel volumes and music crossfades. Depends on Pretzel
Core and the built-in audio module.

## AudioCue

**Assets > Create > Pretzel > Audio > Audio Cue**: one or more clips, a channel, volume and pitch ranges, looping,
3D blend and distances. **Avoid Repeat** never plays the same clip twice in a row.

## AudioService

```csharp
AudioHandle handle = audio.Play(footstepCue);                    // 2D or as set on the cue
audio.Play(explosionCue, hitPoint);                              // at a world position
audio.PlayClip(uiClick, AudioChannel.UI);
audio.Stop(handle);

audio.PlayMusic(battleTheme, fadeSeconds: 1.5f);                 // crossfade on two dedicated sources
audio.StopMusic();

audio.SetVolume(AudioChannel.Music, 0.4f);                       // 0 to 1
```

- **Max Voices** is a fixed budget of pooled AudioSources (internally `AudioEmitter` objects, one per source);
  **Prewarm Voices** are created in `Awake`. When the budget is
  used up, the oldest non-looping sound is stolen. Looping sounds are never stolen.
- An `AudioHandle` goes stale when its sound ends, so stopping an old handle never stops a newer sound.
- Channels: Master, Music, Sfx, UI, Voice, Ambience. Bind a channel to an `AudioMixerGroup` and, optionally, an exposed
  mixer volume parameter. With a parameter, volume goes to the mixer in decibels (`AudioMath.LinearToDecibels`).
  Without one, the service scales AudioSource volume. Master always applies.
- Music fades use unscaled time, so they keep running while the game is paused.

Gameplay code should depend on **IAudioService** (implemented by `AudioService`) instead of the component, so a test can pass a fake that records `Play` calls and needs no AudioSource, mixer or scene.

Pair it with Pretzel Preferences to save volumes, and with your DI container or composition root to share one
service. It is not a singleton on purpose.

## Sample

`Samples~/AudioUsage`: cues, positional sounds, handles, music crossfades and volumes.
