// Prints peak, RMS and the level of the last 5 ms for every WAV in the pack, to catch silence, clipping and end clicks.
const fs = require("fs");
const path = require("path");
const dir = process.argv[2];
for (const file of fs.readdirSync(dir).filter((f) => f.endsWith(".wav"))) {
  const buffer = fs.readFileSync(path.join(dir, file));
  const dataIndex = buffer.indexOf("data") + 8;
  const samples = (buffer.length - dataIndex) >> 1;
  let peak = 0, sum = 0, tailPeak = 0, firstPeak = 0;
  const tail = Math.floor(44100 * 2 * 0.005);
  for (let i = 0; i < samples; i++) {
    const v = Math.abs(buffer.readInt16LE(dataIndex + i * 2)) / 32768;
    peak = Math.max(peak, v);
    sum += v * v;
    if (i >= samples - tail) tailPeak = Math.max(tailPeak, v);
    if (i < 8) firstPeak = Math.max(firstPeak, v);
  }
  const db = (x) => (x > 0 ? (20 * Math.log10(x)).toFixed(1) : "-inf");
  console.log(`${file.padEnd(20)} peak ${db(peak)} dB  rms ${db(Math.sqrt(sum / samples))} dB  start ${db(firstPeak)} dB  tail ${db(tailPeak)} dB`);
}
