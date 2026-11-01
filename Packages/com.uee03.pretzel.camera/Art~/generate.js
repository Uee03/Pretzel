// Writes the VectorCraft command scripts for the Camera Example art. build.ps1 runs this, then VectorCraft.
const fs = require("fs");
const path = require("path");

const out = path.join(__dirname, "scripts");
fs.mkdirSync(out, { recursive: true });
const write = (name, steps) => fs.writeFileSync(path.join(out, name + ".json"), JSON.stringify(steps, null, 1));

const newDoc = (name, width, height) => ({
  command: "file.new",
  params: { name, width, height, units: "Pixels", backgroundContents: "transparent", colorMode: "rgb" },
});
const fill = (color) => [
  { command: "paint.setFill", params: { color } },
  { command: "paint.setStroke", params: { none: true } },
];
const rect = (x, y, width, height, radius) => ({
  command: "shape.rectangle",
  params: radius ? { x, y, width, height, radius } : { x, y, width, height },
});
const ellipse = (cx, cy, rx, ry) => ({ command: "shape.ellipse", params: { x: cx - rx, y: cy - ry, width: rx * 2, height: ry * 2 } });

// The arena is 32 x 18 world units, drawn at 32 pixels per unit: a floor with a one-unit grid and a thick wall.
const W = 1024;
const H = 576;
const arena = [newDoc("camera-arena", W, H), rect(0, 0, W, H), ...fill("#26324a")];
for (let x = 32; x < W; x += 32) {
  arena.push(rect(x - 1, 0, 2, H), ...fill(x % 128 === 0 ? "#3a4a6b" : "#2e3b57"));
}
for (let y = 32; y < H; y += 32) {
  arena.push(rect(0, y - 1, W, 2), ...fill(y % 128 === 0 ? "#3a4a6b" : "#2e3b57"));
}
for (const [x, y, w, h] of [[0, 0, W, 24], [0, H - 24, W, 24], [0, 0, 24, H], [W - 24, 0, 24, H]]) {
  arena.push(rect(x, y, w, h), ...fill("#f2a33a"));
}
for (const [cx, cy] of [[160, 160], [864, 160], [160, 416], [864, 416], [512, 288]]) {
  arena.push(ellipse(cx, cy, 40, 40), ...fill("#3d5a80"), ellipse(cx, cy, 24, 24), ...fill("#98c1d9"));
}
write("arena", arena);

// A glowing orb for the camera to follow.
write("orb", [
  newDoc("camera-orb", 64, 64),
  ellipse(32, 32, 28, 28), ...fill("#ff7b54"),
  ellipse(32, 32, 20, 20), ...fill("#ffb26b"),
  ellipse(26, 25, 8, 6), ...fill("#fff3d6"),
]);
console.log("wrote", out);
