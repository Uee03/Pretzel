// Writes the SoundCraft scripts for the SFX pack. build.ps1 runs this, bounces each script to WAV, and normalizes it.
//
// One tick is 1/960 s at 120 BPM (480 ticks = 0.5 s). Each sound lists its length in seconds for the bounce.
const fs = require("fs");
const path = require("path");

const out = path.join(__dirname, "scripts");
fs.mkdirSync(out, { recursive: true });

const line = (command, params) => `${command} ${JSON.stringify(params)}`;

// A single-instrument session: a synth or drum track, optional inserts, and a list of notes [pitch, startTicks, lengthTicks, velocity].
function sound(name, { instrument = "subtractive_synth", params = {}, inserts = [], notes, format = "mono", seconds }) {
  const lines = [
    `# ${name}`,
    line("session.new", { name, sample_rate: 44100, bit_depth: 16 }),
    line("track.new", { kind: "instrument", name, instrument, format }),
    line("mix.instrument", { track: 1, plugin: instrument, params }),
  ];
  inserts.forEach((insert, index) => lines.push(line("mix.insert", { track: 1, slot: "abcdefghij"[index], plugin: insert.plugin, params: insert.params || {} })));
  lines.push(line("midi.new_clip", { track: 1 }));
  for (const [pitch, start, length, velocity] of notes) {
    lines.push(line("midi.note_add", { clip: 2, pitch, start_ticks: start, length_ticks: length, velocity: velocity || 110 }));
  }
  fs.writeFileSync(path.join(out, name + ".txt"), lines.join("\n") + "\n");
  return { name, seconds };
}

const synth = (extra) => ({ osc1_wave: 2, osc2_wave: 2, osc_mix: 0, cutoff: 12000, resonance: 5, filter_env_amount: 0, amp_attack: 1, amp_decay: 80, amp_sustain: 0, amp_release: 60, velocity: 100, level: -6, ...extra });

const sounds = [
  // UI
  sound("ui_click", { params: synth({ osc1_wave: 2, amp_decay: 35, amp_release: 25 }), notes: [[88, 0, 40]], seconds: 0.25 }),
  sound("ui_confirm", { params: synth({ osc1_wave: 3, amp_decay: 90, amp_sustain: 40, amp_release: 120 }), notes: [[76, 0, 90], [83, 90, 200]], seconds: 0.6 }),
  sound("ui_back", { params: synth({ osc1_wave: 3, amp_decay: 90, amp_sustain: 40, amp_release: 120 }), notes: [[83, 0, 90], [76, 90, 200]], seconds: 0.6 }),
  sound("ui_error", { params: synth({ osc1_wave: 0, cutoff: 700, amp_decay: 120, amp_sustain: 50, amp_release: 100, level: -4 }), inserts: [{ plugin: "lofi" }], notes: [[45, 0, 110], [43, 130, 200]], seconds: 0.7 }),

  // Gameplay
  sound("pickup_coin", { params: synth({ osc1_wave: 1, osc2_wave: 1, cutoff: 9000, resonance: 10, amp_decay: 120, amp_sustain: 60, amp_release: 150 }), notes: [[83, 0, 100], [88, 100, 300]], seconds: 0.7 }),
  sound("power_up", { params: synth({ osc1_wave: 1, osc2_wave: 0, osc_mix: 40, osc2_detune: 15, cutoff: 8000, amp_decay: 120, amp_sustain: 50, amp_release: 180 }), notes: [[72, 0, 70], [76, 70, 70], [79, 140, 70], [84, 210, 320]], seconds: 1.1 }),
  sound("jump", { params: synth({ osc1_wave: 3, glide: 90, amp_decay: 110, amp_release: 80 }), notes: [[60, 0, 40], [72, 40, 120]], seconds: 0.45 }),
  sound("laser", { params: synth({ osc1_wave: 0, osc2_wave: 1, osc_mix: 35, cutoff: 6500, resonance: 25, glide: 200, amp_decay: 200, amp_release: 80 }), notes: [[100, 0, 40], [55, 40, 160]], seconds: 0.5 }),
  sound("hit_light", { instrument: "drum_synth", format: "stereo", params: { snare_decay: 140, snare_snappy: 70, snare_tune: 4 }, notes: [[38, 0, 60, 120]], seconds: 0.5 }),
  sound("hit_heavy", { instrument: "drum_synth", format: "stereo", params: { kick_decay: 300, kick_tune: -4, snare_snappy: 80, snare_decay: 350 }, inserts: [{ plugin: "plate_reverb" }], notes: [[36, 0, 120, 127], [38, 0, 120, 120]], seconds: 1.9 }),
  sound("explosion", { instrument: "drum_synth", format: "stereo", params: { kick_decay: 900, kick_tune: -14, snare_decay: 700, snare_snappy: 100, cymbal_decay: 900 }, inserts: [{ plugin: "room_reverb" }, { plugin: "lofi" }], notes: [[36, 0, 240, 127], [38, 0, 240, 127], [49, 0, 240, 100]], seconds: 2.0 }),

  // Stingers
  sound("stinger_win", { format: "stereo", params: synth({ osc1_wave: 1, osc2_wave: 0, osc_mix: 40, cutoff: 7000, amp_decay: 300, amp_sustain: 40, amp_release: 700 }), inserts: [{ plugin: "room_reverb" }], notes: [[72, 0, 120], [76, 120, 120], [79, 240, 120], [84, 360, 700], [88, 360, 700], [91, 360, 700]], seconds: 2.4 }),
  sound("stinger_lose", { format: "stereo", params: synth({ osc1_wave: 3, cutoff: 3500, amp_decay: 400, amp_sustain: 40, amp_release: 800 }), inserts: [{ plugin: "room_reverb" }], notes: [[67, 0, 200], [64, 240, 200], [60, 480, 700], [55, 480, 700]], seconds: 2.6 }),
];

fs.writeFileSync(path.join(out, "manifest.json"), JSON.stringify(sounds, null, 1));
console.log("wrote", sounds.length, "scripts to", out);
