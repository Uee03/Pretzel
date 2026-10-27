# Audio Usage

| File | What it shows |
|------|---------------|
| `AudioUsageExample.cs` | Playing `AudioCue`s in 2D and at a position, stopping by handle, crossfading music and setting channel volumes. |

## Try it

1. Add `AudioManager` to a GameObject. Optionally assign an `AudioMixer`, and add channel bindings with a mixer
   group and an exposed volume parameter (for example `MusicVolume`) per channel.
2. Create cues with **Assets > Create > Pretzel > Audio > Audio Cue** and add clips. Several clips with
   **Avoid Repeat** give natural footsteps.
3. Add `AudioUsageExample`, assign the manager, the cues and two music clips, press Play and use the context menu.

Rules:

- A channel with an exposed mixer parameter is controlled through the mixer (in decibels, converted from 0 to 1).
  Without one, the manager scales AudioSource volume. Master always applies.
- The voice budget (**Max Voices**) is fixed. When it is used up, the oldest non-looping sound is stolen.
- `AudioHandle` goes stale when its sound ends, so stopping an old handle never stops a newer sound.
- Feed saved volumes in from Pretzel Preferences when they load or change.
