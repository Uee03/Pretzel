// Writes the VectorCraft command scripts for the Parallax Example art.
//
// Every wide layer is periodic: the silhouette is built from sine waves with a whole number of periods across the
// width, so its left and right edges match and the PNG tiles with no seam.
//
//   build.ps1 runs this script, then VectorCraft on the output, and copies the PNGs into the sample.
const fs = require("fs");
const path = require("path");

const out = path.join(__dirname, "scripts");
fs.mkdirSync(out, { recursive: true });

function write(name, steps) {
  fs.writeFileSync(path.join(out, name + ".json"), JSON.stringify(steps, null, 1));
}

// A small deterministic generator, so the art is the same every time the script runs.
function random(seed) {
  let state = seed;
  return () => {
    state = (state * 1664525 + 1013904223) % 4294967296;
    return state / 4294967296;
  };
}

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

// A closed shape whose top edge is a sum of sine waves with whole periods across the width.
function silhouette(width, base, height, waves, color, step = 8) {
  const points = [];
  for (let x = 0; x <= width; x += step) {
    let y = base;
    for (const [periods, amplitude, phase] of waves) {
      y -= amplitude * Math.sin((2 * Math.PI * periods * x) / width + phase);
    }
    points.push(`L${x} ${y.toFixed(1)}`);
  }
  const d = `M0 ${height} ${points.join(" ")} L${width} ${height} Z`;
  return [{ command: "path.create", params: { d } }, ...fill(color)];
}

// Sky: a vertical gradient with clouds that stay clear of the edges, so the tile still repeats cleanly.
const sky = [
  newDoc("parallax-sky", 1024, 512),
  rect(0, 0, 1024, 512),
  {
    command: "paint.setFill",
    params: {
      gradient: {
        kind: "linear",
        angle: 270,
        stops: [
          { offset: 0, color: "#2b3f7a" },
          { offset: 0.55, color: "#6f8fd0" },
          { offset: 1, color: "#f3c98b" },
        ],
      },
    },
  },
  { command: "paint.setStroke", params: { none: true } },
];
for (const [cx, cy, scale] of [[190, 120, 1.0], [520, 200, 0.7], [850, 90, 0.9], [700, 330, 0.5], [330, 340, 0.6]]) {
  for (const [dx, dy, rx, ry] of [[0, 0, 70, 26], [-48, 8, 46, 20], [52, 10, 52, 20], [6, -14, 44, 24]]) {
    sky.push(ellipse(cx + dx * scale, cy + dy * scale, rx * scale, ry * scale), ...fill("#ffffff"));
  }
}
write("sky", sky);

// Far mountains, hills and trees, each darker and lower than the one behind it.
write("mountains", [newDoc("parallax-mountains", 1024, 256), ...silhouette(1024, 150, 256, [[2, 60, 0.4], [5, 26, 1.3], [11, 8, 2.1]], "#56638f")]);
write("hills", [newDoc("parallax-hills", 1024, 192), ...silhouette(1024, 110, 192, [[3, 30, 0.2], [7, 12, 2.4]], "#3b7a68")]);

const next = random(3);
const trees = [newDoc("parallax-trees", 1024, 192), rect(0, 168, 1024, 24), ...fill("#1d4a3a")];
for (let x = 40; x < 980; x += 60 + Math.floor(next() * 50)) {
  const height = 70 + Math.floor(next() * 60);
  trees.push({ command: "shape.polygon", params: { cx: x, cy: 162 - height / 4, radius: height / 2, sides: 3 } }, ...fill("#1d4a3a"));
  trees.push(rect(x - 4, 160, 8, 14), ...fill("#14362b"));
}
write("trees", trees);

// Ground strip, 256 wide, with a grass edge.
const ground = [newDoc("parallax-ground", 256, 192), rect(0, 0, 256, 192), ...fill("#6b4a2f"), rect(0, 0, 256, 18), ...fill("#5fae4a")];
for (let y = 40; y < 192; y += 52) {
  for (let x = (y % 2) * 16; x < 256; x += 64) {
    ground.push(rect(x + 6, y, 14, 8, 3), ...fill("#5a3d27"));
  }
}
write("ground", ground);

// A small runner for the camera to follow.
write("runner", [
  newDoc("parallax-runner", 64, 64),
  ellipse(32, 36, 22, 20), ...fill("#f2a33a"),
  ellipse(40, 30, 5, 6), ...fill("#ffffff"),
  ellipse(42, 31, 2.4, 3), ...fill("#222222"),
  rect(18, 52, 10, 8, 3), ...fill("#c77b1d"),
  rect(36, 52, 10, 8, 3), ...fill("#c77b1d"),
]);

console.log("wrote", out);
