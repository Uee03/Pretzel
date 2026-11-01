---
name: soundcraft-audio
description: "Make game sound effects, jingles and short music with SoundCraft's headless CLI: build a session with synth or drum tracks, add MIDI notes, insert effects, bounce to WAV, normalize and convert for Unity. Use for pickups, UI clicks, hits, power-ups, stingers and placeholder audio for Pretzel samples and games. Load craft-tools first."
---

# SoundCraft: sound effects and music

SoundCraft is a DAW (tracks, clips, MIDI, plugins, mixing). The CLI runs its engine headless:
`C:\Program Files\SoundCraft\soundcraft-cli.exe` (0.14.0). Read `craft-tools` first for locations, quoting and safety.

## Workflow

1. Write a **script file**: one command per line as `command.id {json}`; blank lines and `#` comments are skipped; a failing line stops the run (`--keep-going` continues).
2. Run it and bounce in one go:
   `soundcraft-cli script s.txt --bounce out.wav --start 0 --end 0.7 [--save s.scraft] [--inspect --compact]`
3. Convert for Unity: `soundcraft-cli convert out.wav sfx.wav --bit-depth 16 --normalize` (also `--rate 44100`).
4. Check: `soundcraft-cli info sfx.wav --compact` gives channels, rate, frames, seconds. `bounce` prints `peak_db`, `lufs`, `true_peak_db`.
5. Report the numbers and that it was not listened to.

`--bounce` writes 24-bit stereo by default and the raw bounce is quiet (the drum example peaked at about -11 dBFS), so always
`convert --normalize` (peak to -0.1 dBFS) for the delivered file. `--start/--end` take seconds (`0.7`) or a time string.
Pick `--end` after the last note's **release/reverb tail**, or it is cut.

## The session model (verified)

- `session.new {"name","sample_rate":44100,"bit_depth":16}`. Default tempo 120 BPM, 4/4; **960 ticks = one quarter note = 0.5 s** (measured in 0.14.0: a note at tick 9600 sounds at 5 s; do not assume 480). One bar = 2 s. `event.tempo {"bpm"}` changes the tempo; set it before creating clips.
- `track.new {"kind":"instrument","name","instrument":"<plugin id>","format":"mono|stereo"}` returns `{"tracks":[N]}`; the first track is `1`.
- `mix.instrument {"track":1,"plugin":"subtractive_synth","params":{...}}` sets the instrument and its parameters (values are clamped into range; **unknown ids are errors**).
- `midi.new_clip {"track":1}` returns `{"clip":N,...}`; ids share one counter, so with T tracks created first the clips are T+1, T+2... Default length is 1 bar; **`end` is in samples** (`{"track":1,"start":0,"end":1058400}`); `length` is ignored.
- `midi.note_add {"clip":2,"pitch":83,"start_ticks":0,"length_ticks":100,"velocity":110}` (pitch is MIDI 0-127; 60 = C4, 69 = A4 = 440 Hz). `midi.notes`, `midi.note_edit`, `midi.note_delete`, `midi.notes_move` exist.
- `mix.insert {"track":1,"slot":"a","plugin":"plate_reverb","params":{...}}` adds an effect (slots `0..9` or `a..j`); `mix.insert_param {track,slot,param,value}` changes it.
- `mix.volume {"tracks":..,"db":-3}`, `mix.pan`, `mix.mute`, `mix.send`, `mix.new_bus` exist. `automation.set_point {track,param: volume|pan|mute|plugin:a:<id>,at,value}` automates a track or **insert** parameter (instrument parameters are set statically with `mix.instrument_param`).
- File commands: `file.import_audio`, `file.import_midi`, `file.bounce_mix {path, format: wav|aiff|flac, bit_depth, start, end, normalize, dither}` (same as the `--bounce` flag), `file.bounce_stems`, `session.save`.
- Edit commands (`edit.trim_to_selection`, `edit.fades_create`, `edit.strip_silence`, `clip.loop`, `clip.gain_render`, `event.tempo_stretch`, pitch shift) operate on imported audio too; run `soundcraft-cli commands <word>` and `describe <id>` before using one.

## Built-in plugins (`soundcraft-cli plugins`)

Instruments: `subtractive_synth`, `drum_synth`, `sampler`. Generator: `signal_generator`.
Effects: EQ (`eq_1band`, `eq_7band`), dynamics (`compressor`, `expander_gate`, `de_esser`, `maximizer`, `channel_strip`), reverb (`room_reverb`, `plate_reverb`),
`mod_delay`, modulation (`auto_pan`, `chorus`, `flanger`, `phaser`, `ring_mod`), harmonic (`saturator`, `lofi`, `rectifier`, `subharmonic`),
`pitch_shifter`, `time_shift`, `gain`, `trim`, `invert`, `dc_offset_removal`, `dither`. CLAP and VST3 plugins also load (`engine.clap_plugins`, `engine.vst3_plugins`).
Get an effect's parameter ids with the `engine.plugins` command in a script (JSON; parse it with `ConvertFrom-Json`).

### `subtractive_synth` parameters (ids, range, default)

`osc1_wave`, `osc2_wave` (0 Saw, 1 Square, 2 Sine, 3 Triangle; default 0), `osc2_semitones` (-24..24), `osc2_detune` (-100..100 cents, 7), `osc_mix` (0..100, 50; 0 = osc1 only),
`cutoff` (20..20000 Hz, 2000), `resonance` (0..100, 20), `filter_env_amount` (-100..100, 30), `amp_attack` (0.5..5000 ms, 5), `amp_decay` (1..5000, 200), `amp_sustain` (0..100, 70), `amp_release` (1..10000, 300),
`filter_attack/decay/sustain/release`, `glide` (0..1000 ms), `velocity` (0..100, 70), `level` (-48..6 dB, -6).

### `drum_synth` parameters

`level`, `kick_tune`, `kick_decay`, `snare_tune`, `snare_snappy`, `snare_decay`, `hat_decay`, `open_hat_decay`, `tom_tune`, `tom_decay`, `cymbal_decay`.
Notes follow the General MIDI drum map: **36 kick, 38 snare** verified to sound; use 42/46 hats, 41-50 toms, 49/51 cymbals (untested).

### `sampler` and `signal_generator`

`sampler`: `root_note`, `transpose`, `fine_tune`, `play_mode` (0 Gate, 1 One Shot), `attack/decay/sustain/release`, `loop_mode`, key and velocity ranges; load audio with `mix.sampler_load`, clear with `mix.sampler_clear`.
`signal_generator`: `waveform` (0 Sine, 1 Square, 2 Saw, 3 Triangle, 4 White Noise, 5 Pink Noise), `freq` (20..20000), `level` (-96..0 dB). Use it as an insert for a noise burst or test tone.

## Recipes (verified, run from `reference/`)

- `reference/coin-pickup.txt`: two square notes, B5 (83) then E6 (88), short amp envelope: a classic pickup. 0.7 s.
- `reference/impact-drums.txt`: kick (36) + snare (38) on `drum_synth` through `plate_reverb`: a heavy hit with a tail. 1.2 s.

Design ideas that use the same commands (untested, adapt and listen): **UI click** = one 30 ms sine/square note, `amp_decay` 40, no reverb;
**power-up** = ascending arpeggio (e.g. 72, 76, 79, 84, each 60 ticks) with `osc2_detune` 15; **error buzz** = low saw (45) with `cutoff` 600 and a `lofi` insert;
**stinger** = chord (three notes at once) with `room_reverb`; **whoosh/explosion** = `signal_generator` Pink Noise insert with volume automation, then `lofi`.
Keep SFX short (0.1-1.5 s), end with a release so there is no click, and leave about -1 dBFS headroom.

## Gotchas

- PowerShell 5.1 and inline `--cmd 'id={"a":1}'`: quotes are stripped, so use `script` with a file. (`run --cmd ID=JSON` was not tried from other shells.)
- A new `session.new` per script. To edit a saved session use `--in s.scraft` and either `--save other.scraft` or `--in-place`.
- Ids: first track 1, first clip 2 (observed in a fresh session). Read each line's printed result before using an id later.
- A command can be disabled by state (`describe` shows `enabled` and `disabled_reason`, e.g. "nothing is selected" when `track` is omitted); pass `track` explicitly.
- The headless engine has no audio device: bouncing is offline, so there is nothing to hear. Say the output was not auditioned.
