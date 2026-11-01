// Writes the SoundCraft scripts for the three background tunes. build-music.ps1 runs this, bounces each script,
// and loopwrap.js folds the reverb tail back onto the start so the WAV loops without a gap.
// Each tune is 12 bars: bars 1-4 are the bed, 5-8 add the arpeggio, 9-12 add the hook. Layers use fromBar (1-based).
const fs = require("fs");
const path = require("path");

const out = path.join(__dirname, "scripts");
fs.mkdirSync(out, { recursive: true });
const line = (command, params) => `${command} ${JSON.stringify(params)}`;

// Note positions below are written for 480 ticks per quarter note. SoundCraft 0.14.0 actually runs 960 per quarter
// (measured: a note at tick 9600 sounds at 5 s at 120 BPM), so every tick is multiplied by TICK_SCALE on output.
const BAR = 1920;
const BEAT = 480;
const TICK_SCALE = 2;
const SAMPLE_RATE = 44100;
const BARS = 12;
const MINOR = [0, 3, 7, 12];
const MAJOR = [0, 4, 7, 12];

// A progression repeats every four bars. root is the bass note in MIDI, chord the interval set.
const tunes = [
  {
    name: "title_theme",
    bpm: 90,
    chords: [
      { root: 33, chord: MINOR },
      { root: 29, chord: MAJOR },
      { root: 36, chord: MAJOR },
      { root: 31, chord: MAJOR },
    ],
    hook: [
      [[76, 0, 2], [79, 2, 1], [76, 3, 1]],
      [[77, 0, 2], [81, 2, 1], [77, 3, 1]],
      [[79, 0, 2], [84, 2, 1], [79, 3, 1]],
      [[74, 0, 1], [79, 1, 1], [83, 2, 2]],
    ],
    layers: [
      { name: "pad", kind: "pad", fromBar: 1, octave: 24, velocity: 78,
        synth: { osc1_wave: 0, osc2_wave: 0, osc2_detune: 14, osc_mix: 50, cutoff: 1300, resonance: 10, filter_env_amount: 10, amp_attack: 500, amp_decay: 500, amp_sustain: 85, amp_release: 700, level: -14 }, reverb: true },
      { name: "bass", kind: "bass_whole", fromBar: 1,
        synth: { osc1_wave: 2, osc2_wave: 3, osc2_semitones: -12, osc_mix: 40, cutoff: 600, resonance: 5, filter_env_amount: 0, amp_attack: 20, amp_decay: 300, amp_sustain: 80, amp_release: 300, level: -7 } },
      { name: "arp", kind: "arp8", fromBar: 5, octave: 36, velocity: 72,
        synth: { osc1_wave: 3, osc2_wave: 2, osc2_semitones: 12, osc_mix: 30, cutoff: 3200, resonance: 10, filter_env_amount: 20, amp_attack: 3, amp_decay: 220, amp_sustain: 0, amp_release: 120, level: -10 }, reverb: true },
      { name: "hook", kind: "hook", fromBar: 9, velocity: 90,
        synth: { osc1_wave: 2, osc2_wave: 3, osc_mix: 40, cutoff: 3500, resonance: 5, filter_env_amount: 10, amp_attack: 30, amp_decay: 400, amp_sustain: 70, amp_release: 500, level: -10 }, reverb: true },
    ],
  },
  {
    name: "gameplay_loop",
    bpm: 120,
    chords: [
      { root: 40, chord: MINOR },
      { root: 36, chord: MAJOR },
      { root: 43, chord: MAJOR },
      { root: 38, chord: MAJOR },
    ],
    hook: [
      [[76, 0, 1], [79, 1, 1], [83, 2, 2]],
      [[79, 0, 1], [76, 1, 1], [84, 2, 2]],
      [[74, 0, 1], [79, 1, 1], [83, 2, 2]],
      [[78, 0, 1], [74, 1, 1], [81, 2, 2]],
    ],
    layers: [
      { name: "drums", kind: "drums_mid", fromBar: 1, synth: { level: -6 } },
      { name: "bass", kind: "bass8", fromBar: 1,
        synth: { osc1_wave: 0, osc2_wave: 1, osc2_semitones: -12, osc_mix: 40, cutoff: 520, resonance: 15, filter_env_amount: 30, amp_attack: 2, amp_decay: 150, amp_sustain: 40, amp_release: 60, level: -8 } },
      { name: "pad", kind: "pad", fromBar: 1, octave: 24, velocity: 60,
        synth: { osc1_wave: 0, osc2_wave: 0, osc2_detune: 12, osc_mix: 50, cutoff: 1100, resonance: 8, filter_env_amount: 5, amp_attack: 200, amp_decay: 400, amp_sustain: 80, amp_release: 300, level: -16 } },
      { name: "arp", kind: "arp16", fromBar: 5, octave: 24, velocity: 70,
        synth: { osc1_wave: 1, osc2_wave: 3, osc2_semitones: 12, osc_mix: 35, cutoff: 2600, resonance: 12, filter_env_amount: 25, amp_attack: 2, amp_decay: 110, amp_sustain: 0, amp_release: 60, level: -13 } },
      { name: "hook", kind: "hook", fromBar: 9, velocity: 100,
        synth: { osc1_wave: 1, osc2_wave: 0, osc2_detune: 8, osc_mix: 35, cutoff: 2800, resonance: 10, filter_env_amount: 20, amp_attack: 5, amp_decay: 250, amp_sustain: 50, amp_release: 180, level: -12 }, reverb: true },
    ],
  },
  {
    name: "last_stand",
    bpm: 150,
    chords: [
      { root: 38, chord: MINOR },
      { root: 34, chord: MAJOR },
      { root: 41, chord: MAJOR },
      { root: 36, chord: MAJOR },
    ],
    hook: [
      [[81, 0, 1], [77, 1, 1], [74, 2, 2]],
      [[82, 0, 1], [79, 1, 1], [74, 2, 2]],
      [[81, 0, 1], [77, 1, 1], [72, 2, 2]],
      [[79, 0, 1], [76, 1, 1], [84, 2, 2]],
    ],
    layers: [
      { name: "drums", kind: "drums_hard", fromBar: 1, synth: { level: -5 } },
      { name: "bass", kind: "bass16", fromBar: 1,
        synth: { osc1_wave: 0, osc2_wave: 1, osc2_semitones: -12, osc_mix: 45, cutoff: 700, resonance: 20, filter_env_amount: 35, amp_attack: 1, amp_decay: 90, amp_sustain: 10, amp_release: 40, level: -9 } },
      { name: "stab", kind: "pad", fromBar: 1, octave: 24, velocity: 70,
        synth: { osc1_wave: 0, osc2_wave: 0, osc2_detune: 18, osc_mix: 50, cutoff: 1800, resonance: 10, filter_env_amount: 20, amp_attack: 20, amp_decay: 400, amp_sustain: 70, amp_release: 200, level: -17 } },
      { name: "arp", kind: "arp8", fromBar: 5, octave: 36, velocity: 85,
        synth: { osc1_wave: 0, osc2_wave: 1, osc2_semitones: 12, osc_mix: 30, cutoff: 2400, resonance: 18, filter_env_amount: 30, amp_attack: 1, amp_decay: 120, amp_sustain: 0, amp_release: 50, level: -12 } },
      { name: "hook", kind: "hook", fromBar: 9, velocity: 105,
        synth: { osc1_wave: 0, osc2_wave: 1, osc_mix: 40, cutoff: 3000, resonance: 15, filter_env_amount: 25, amp_attack: 3, amp_decay: 300, amp_sustain: 60, amp_release: 150, level: -11 }, reverb: true },
    ],
  },
];

// Each kind returns notes as [pitch, startTick, lengthTicks, velocity] for one bar of the progression.
function barNotes(kind, tune, barIndex, layer) {
  const entry = tune.chords[barIndex % tune.chords.length];
  const notes = [];
  const vel = layer.velocity || 100;
  switch (kind) {
    case "pad":
      entry.chord.slice(0, 3).forEach((semitone) => notes.push([entry.root + (layer.octave || 24) + semitone, 0, BAR - 40, vel]));
      break;
    case "bass_whole":
      notes.push([entry.root, 0, BAR - 60, 105]);
      break;
    case "bass8":
      [0, 0, 12, 0, 0, 0, 12, 0].forEach((semitone, i) => notes.push([entry.root + semitone, i * 240, 200, i % 2 === 0 ? 115 : 90]));
      break;
    case "bass16":
      [0, 0, 12, 0, 0, 0, 12, 0, 0, 0, 12, 0, 0, 12, 0, 12].forEach((semitone, i) => notes.push([entry.root + semitone, i * 120, 100, i % 4 === 0 ? 120 : 95]));
      break;
    case "arp8":
      [0, 1, 2, 3, 2, 1, 2, 1].forEach((index, i) => notes.push([entry.root + (layer.octave || 36) + entry.chord[index], i * 240, 220, i === 0 ? vel + 15 : vel]));
      break;
    case "arp16":
      [0, 1, 2, 3, 2, 1, 2, 1, 0, 1, 2, 3, 2, 1, 3, 2].forEach((index, i) => notes.push([entry.root + (layer.octave || 24) + 12 + entry.chord[index] - 12, i * 120, 100, i % 4 === 0 ? vel + 15 : vel]));
      break;
    case "hook":
      tune.hook[barIndex % tune.hook.length].forEach(([pitch, beat, beats]) => notes.push([pitch, beat * BEAT, beats * BEAT - 30, vel]));
      break;
    case "drums_mid":
      [0, 960].forEach((tick) => notes.push([36, tick, 120, 120]));
      if (barIndex % 2 === 1) {
        notes.push([36, 1680, 120, 100]);
      }
      [480, 1440].forEach((tick) => notes.push([38, tick, 120, 110]));
      for (let i = 0; i < 8; i++) {
        notes.push([42, i * 240, 60, i % 2 === 0 ? 85 : 60]);
      }
      break;
    case "drums_hard":
      [0, 480, 960, 1440].forEach((tick) => notes.push([36, tick, 100, 125]));
      [480, 1440].forEach((tick) => notes.push([38, tick, 100, 118]));
      for (let i = 0; i < 16; i++) {
        notes.push([42, i * 120, 50, i % 4 === 0 ? 95 : i % 2 === 0 ? 75 : 55]);
      }
      if (barIndex % 4 === 0) {
        notes.push([49, 0, 480, 100]);
      }
      if (barIndex % 4 === 3) {
        [1680, 1740, 1800, 1860].forEach((tick) => notes.push([38, tick, 50, 105]));
      }
      break;
    default:
      throw new Error("unknown kind " + kind);
  }

  return notes;
}

const summary = [];
for (const tune of tunes) {
  const lines = [`# ${tune.name}: ${tune.bpm} BPM, ${BARS} bars`];
  lines.push(line("session.new", { name: tune.name, sample_rate: SAMPLE_RATE, bit_depth: 16 }));
  lines.push(line("event.tempo", { bpm: tune.bpm }));

  // Tracks take ids 1..N, then the clips take N+1... in creation order.
  tune.layers.forEach((layer) => {
    const drum = layer.kind.startsWith("drums");
    lines.push(line("track.new", { kind: "instrument", name: layer.name, instrument: drum ? "drum_synth" : "subtractive_synth", format: "stereo" }));
  });
  const loopSeconds = (BARS * 4 * 60) / tune.bpm;
  const endSample = Math.round(loopSeconds * SAMPLE_RATE);
  tune.layers.forEach((layer, i) => {
    const drum = layer.kind.startsWith("drums");
    lines.push(line("mix.instrument", { track: i + 1, plugin: drum ? "drum_synth" : "subtractive_synth", params: layer.synth }));
    if (layer.reverb) {
      lines.push(line("mix.insert", { track: i + 1, slot: "a", plugin: "plate_reverb" }));
    }
  });
  tune.layers.forEach((layer, i) => lines.push(line("midi.new_clip", { track: i + 1, start: 0, end: endSample })));

  let noteCount = 0;
  tune.layers.forEach((layer, i) => {
    const clip = tune.layers.length + 1 + i;
    for (let bar = layer.fromBar - 1; bar < BARS; bar++) {
      for (const [pitch, start, length, velocity] of barNotes(layer.kind, tune, bar, layer)) {
        lines.push(line("midi.note_add", { clip, pitch, start_ticks: (bar * BAR + start) * TICK_SCALE, length_ticks: length * TICK_SCALE, velocity }));
        noteCount++;
      }
    }
  });

  fs.writeFileSync(path.join(out, tune.name + ".txt"), lines.join("\n") + "\n");
  summary.push({ name: tune.name, bpm: tune.bpm, loopSeconds, notes: noteCount });
}

fs.writeFileSync(path.join(out, "music.json"), JSON.stringify(summary, null, 2));
console.log(JSON.stringify(summary));
