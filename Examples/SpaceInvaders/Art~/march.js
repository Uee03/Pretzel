// Writes the SoundCraft scripts for the four march notes: short low square thuds on A2, G2, F2 and E2 (110, 98, 87.3, 82.4 Hz).
// build-audio.ps1 runs this, bounces each script to WAV and normalizes it.
const fs = require("fs");
const path = require("path");

const out = path.join(__dirname, "scripts");
fs.mkdirSync(out, { recursive: true });
const line = (command, params) => `${command} ${JSON.stringify(params)}`;

const notes = [45, 43, 41, 40];
notes.forEach((pitch, index) => {
  const name = `march_${index + 1}`;
  const lines = [
    `# ${name}`,
    line("session.new", { name, sample_rate: 44100, bit_depth: 16 }),
    line("track.new", { kind: "instrument", name, instrument: "subtractive_synth", format: "mono" }),
    line("mix.instrument", {
      track: 1,
      plugin: "subtractive_synth",
      params: { osc1_wave: 1, osc2_wave: 2, osc_mix: 35, cutoff: 700, resonance: 8, filter_env_amount: 20, amp_attack: 1, amp_decay: 90, amp_sustain: 0, amp_release: 60, velocity: 100, level: -3 },
    }),
    line("midi.new_clip", { track: 1 }),
    line("midi.note_add", { clip: 2, pitch, start_ticks: 0, length_ticks: 90, velocity: 120 }),
  ];
  fs.writeFileSync(path.join(out, name + ".txt"), lines.join("\n") + "\n");
});

console.log("wrote", notes.length, "scripts to", out);
