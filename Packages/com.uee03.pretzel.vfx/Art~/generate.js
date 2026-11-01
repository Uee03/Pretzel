// Writes the VectorCraft command script for the Game Feel Example art. build.ps1 runs this, then VectorCraft.
const fs = require("fs");
const path = require("path");

const out = path.join(__dirname, "scripts");
fs.mkdirSync(out, { recursive: true });

const newDoc = (name, width, height) => ({
  command: "file.new",
  params: { name, width, height, units: "Pixels", backgroundContents: "transparent", colorMode: "rgb" },
});
const fill = (color) => [
  { command: "paint.setFill", params: { color } },
  { command: "paint.setStroke", params: { none: true } },
];
const ellipse = (cx, cy, rx, ry) => ({ command: "shape.ellipse", params: { x: cx - rx, y: cy - ry, width: rx * 2, height: ry * 2 } });
const rect = (x, y, width, height, radius) => ({ command: "shape.rectangle", params: { x, y, width, height, radius } });

// A white face, so a tint flash shows clearly: the flash colour multiplies the sprite.
const face = [
  newDoc("gamefeel-face", 128, 128),
  ellipse(64, 64, 58, 58), ...fill("#ffffff"),
  ellipse(64, 64, 50, 50), ...fill("#e9eef7"),
  ellipse(44, 54, 9, 12), ...fill("#2b3550"),
  ellipse(84, 54, 9, 12), ...fill("#2b3550"),
  rect(42, 84, 44, 8, 4), ...fill("#2b3550"),
];
fs.writeFileSync(path.join(out, "face.json"), JSON.stringify(face, null, 1));
console.log("wrote", out);
