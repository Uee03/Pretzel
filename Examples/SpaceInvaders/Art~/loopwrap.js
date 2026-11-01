// Folds the part of a bounce that runs past the loop length (the reverb and release tails) back onto the start, so the
// file loops without a gap or a click, then scales to -1 dBFS and writes 16-bit PCM.
// Usage: node loopwrap.js in.wav loopSeconds out.wav
const fs = require("fs");

const [input, loopSecondsText, output] = process.argv.slice(2);
const buffer = fs.readFileSync(input);

let format = null;
let dataOffset = 0;
let dataLength = 0;
for (let offset = 12; offset + 8 <= buffer.length;) {
  const id = buffer.toString("ascii", offset, offset + 4);
  const size = buffer.readUInt32LE(offset + 4);
  if (id === "fmt ") {
    format = { tag: buffer.readUInt16LE(offset + 8), channels: buffer.readUInt16LE(offset + 10), rate: buffer.readUInt32LE(offset + 12), bits: buffer.readUInt16LE(offset + 22) };
  } else if (id === "data") {
    dataOffset = offset + 8;
    dataLength = Math.min(size, buffer.length - dataOffset);
    break;
  }

  offset += 8 + size + (size % 2);
}

if (!format || format.bits !== 16) {
  throw new Error("expected 16-bit PCM, got " + JSON.stringify(format));
}

const channels = format.channels;
const totalFrames = Math.floor(dataLength / (2 * channels));
const loopFrames = Math.round(parseFloat(loopSecondsText) * format.rate);
if (totalFrames < loopFrames) {
  throw new Error(`bounce has ${totalFrames} frames, loop needs ${loopFrames}`);
}

const samples = new Float64Array(loopFrames * channels);
for (let frame = 0; frame < totalFrames; frame++) {
  const target = frame % loopFrames;
  for (let c = 0; c < channels; c++) {
    samples[target * channels + c] += buffer.readInt16LE(dataOffset + (frame * channels + c) * 2) / 32768;
  }
}

let peak = 0;
for (const value of samples) {
  peak = Math.max(peak, Math.abs(value));
}

const gain = peak > 0 ? 0.891 / peak : 1;
const pcm = Buffer.alloc(samples.length * 2);
samples.forEach((value, i) => pcm.writeInt16LE(Math.max(-32768, Math.min(32767, Math.round(value * gain * 32767))), i * 2));

const header = Buffer.alloc(44);
header.write("RIFF", 0);
header.writeUInt32LE(36 + pcm.length, 4);
header.write("WAVEfmt ", 8);
header.writeUInt32LE(16, 16);
header.writeUInt16LE(1, 20);
header.writeUInt16LE(channels, 22);
header.writeUInt32LE(format.rate, 24);
header.writeUInt32LE(format.rate * channels * 2, 28);
header.writeUInt16LE(channels * 2, 32);
header.writeUInt16LE(16, 34);
header.write("data", 36);
header.writeUInt32LE(pcm.length, 40);
fs.writeFileSync(output, Buffer.concat([header, pcm]));

// Loudness per bar-sized slice, as a check that every section made sound.
const slice = Math.floor(loopFrames / 12);
const levels = [];
for (let s = 0; s < 12; s++) {
  let sum = 0;
  for (let i = s * slice * channels; i < (s + 1) * slice * channels; i++) {
    sum += (samples[i] * gain) ** 2;
  }

  levels.push((10 * Math.log10(sum / (slice * channels) + 1e-12)).toFixed(1));
}

console.log(`${output}: ${(loopFrames / format.rate).toFixed(2)} s, tail folded ${((totalFrames - loopFrames) / format.rate).toFixed(2)} s, rms dB per bar [${levels.join(", ")}]`);
