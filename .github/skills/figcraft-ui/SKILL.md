---
name: figcraft-ui
description: "Design game and app UI with FigCraft's headless CLI: frames with auto layout, buttons, panels, text, effects and components, then render or export PNG/SVG/PDF. Use for UI mockups, button and panel sprites, HUD and menu layouts, and visual references for Unity UI Toolkit screens in Pretzel and other games. Load craft-tools first."
---

# FigCraft: UI design

FigCraft is a Figma-like design app (frames, auto layout, components, styles, variables, prototyping, dev inspect).
`C:\Program Files\FigCraft\figcraft-cli.exe` (0.10.0) runs the same command engine headless; the catalogue has 569 commands.
Read `craft-tools` first for locations, quoting and safety.

## Workflow

1. Write a **JSON array** of `{"command": id, "params": {...}}` steps to a file (or JSON Lines).
2. Run: `figcraft-cli run step.json [--in prev.figcraft] --out next.figcraft`. Each step prints `{"command","ok","result","step"}`.
   **The run is atomic**: if one step fails the document is rolled back and no `--out` is written (the error names the step).
3. Render or export:
   - `figcraft-cli render file.figcraft -o out.png [--scale 2] [--page NAME]` (a quick preview of the page).
   - `figcraft-cli export file.figcraft -o outdir --format png|jpg|svg|webp|pdf [--scale N] [--node ID|NAME] [--no-system-fonts]` writes **one file per top-level frame** into the directory (or one file when `-o` ends in an image extension or `--node` is given). PDFs take `--bleed`, `--crop-marks`, `--cmyk`.
   - `figcraft-cli info file.figcraft --json` lists pages, frames (id, name, size), missing fonts and styles.
4. **View the PNG** and iterate. Use `--no-system-fonts` for committed assets so text renders the same everywhere.
5. Commit the `.figcraft` source and the step files with the exports.

Files: `.figcraft` native, `.json`, `.fig` (best effort; `info` lists what was not imported), `.svg`, and images (open as an image layer). `convert <in> <out>` changes formats.

## Model (verified)

- `file.new {name}`; the first created node is id `2`, then 3, 4... **FigCraft does not accept `"$1.id"` references.** Create the parent in one `run`, then pass its id (`"parent": 2`) in the next run with `--in`/`--out`, or predict the id (ids are sequential per document).
- `node.create {type: rectangle|ellipse|polygon|star|line|arrow|vector|text|frame|section|component|slice, x,y,width,height, parent, name, text, path, props:{node.set keys}}` returns `{"id":N}`. `x`,`y` are relative to the parent frame.
- `node.set` keys (also usable in `props`): `name, visible, locked, opacity, blend, x, y, width, height, rotation, fill (colour) | fills (paints; gradients `{type: linearGradient|radialGradient|angularGradient|diamondGradient, stops:["#hex"..], from, to}`), stroke, strokeWeight, strokeAlign, cornerRadius (n or [tl,tr,br,bl]), cornerSmoothing, effects, clip, isMask`,
  **auto layout:** `layout: horizontal|vertical|grid`, `gap`, `padding`, `mainAlign: min|center|max|spaceBetween|spaceAround|spaceEvenly`, `crossAlign`, `wrap`, `sizingH/sizingV: fixed|hug|fill`, `absolute`, `minWidth/maxWidth/...`, `constraints`,
  **text:** `characters, fontFamily, fontStyle, fontSize, lineHeight, letterSpacing, textAlign, textAutoResize, textCase, maxLines`.
- Other families: `fill.add|set`, `stroke.add|set`, `effect.add {type: dropShadow|innerShadow|layerBlur|backgroundBlur|noise|texture|glass}`, `layout.add`, `image.place {path,x,y,mode}`, `export.add {setting:{size:"2x",suffix,format}}` (per-node export presets), `style.*`, `variable.*` (design tokens), `component.*` and `instance.*` (buttons with variants), `bool.*`, `align.*`, `arrange.*`, `page.*`, `inspect.code {language: css|html|react|...}`, `inspect.properties`, `file.export / file.exportLayers / file.exportSvg / file.exportFramesPdf`, `proto.*` (prototype flows), `text.*` (56 text commands).
- `node.get {id}` and `select.layersAt {x,y}` read a node; `component.list`, `style.list`, `variable.list`, `page.list` list definitions.

## Recipe (verified): a button in two passes

```
figcraft-cli run reference/button-1-frame.json --out button1.figcraft
figcraft-cli run reference/button-2-text.json --in button1.figcraft --out button.figcraft
figcraft-cli export button.figcraft -o frames --format png --scale 2 --no-system-fonts   # frames\Button.png
```

Pass 1 makes a blue 240x72 frame with corner radius 16 and a centered horizontal auto layout; pass 2 adds the white "PLAY" text as child `parent: 2`. It rendered correctly (480x144 PNG at 2x).

## UI art for Unity

- Design each reusable piece as its **own top-level frame** named after the asset (`Button`, `Panel`, `Toast`): `export` writes one PNG per frame named by the frame.
- For buttons and panels that scale, keep the corner radius and border thickness constant and export small (for example 96x96 with 24 px corners), then slice in USS with `-unity-slice-left/top/right/bottom` (UI Toolkit) or the Sprite Editor (UGUI). Draw states (normal, hover, pressed, disabled) as separate frames with the same size.
- Use auto layout frames for HUD and menu mockups so spacing is exact; hand the layout to the UXML/USS author with `inspect.properties` / `inspect.code {language:"css"}` (CSS output is a starting point, not USS: USS lacks several CSS features).
- Tokens (colors, spacing, radii) belong in `variable.*`; mirror them as USS variables (`--color-primary`) in the game.
- Export at 1x and 2x; keep the `.figcraft` source in `Art/Source` and PNGs in the project.
- For icons use `vectorcraft-art`; for sound use `soundcraft-audio`.

## Gotchas

- No step references: use a second run with `--in`/`--out` (or predict ids) for children.
- `export` writes one file per top-level frame; loose shapes outside a frame are not exported unless you pass `--node`.
- Fonts: without `--no-system-fonts`, installed fonts are indexed on first use and renders differ between machines. `info` reports `missingFonts`.
- A failed step rolls the whole run back, which is convenient but means a long script fails as one unit: build in small runs.
- stderr prints `wrote <file>`; PowerShell shows it as an error record. Check `$LASTEXITCODE`.
- `plugin publish`, `collab`, `library`, `proto` and `branch` commands talk to servers or other users; do not run them without being asked.
