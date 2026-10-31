---
name: vectorcraft-art
description: "Draw icons, logos, sprites and illustrations with VectorCraft's headless CLI: create a document, add shapes, paths, gradients, strokes and text, then export SVG and PNG (or .vectorcraft source). Use for game icons, UI glyphs, badges, placeholder art and sample art for Pretzel and other Unity games. Load craft-tools first."
---

# VectorCraft: vector art

VectorCraft is a vector illustration app (paths, Pathfinder, gradients, live effects, artboards, type).
`C:\Program Files\VectorCraft\vectorcraft-cli.exe` (0.16.0) runs the same command engine headless; the catalogue has 802 commands.
Read `craft-tools` first for locations, quoting and safety.

## Workflow

1. Write a **JSON array** of `{"command": id, "params": {...}}` steps to a file (or JSON Lines; max 64 MiB, 100000 steps).
2. Run: `vectorcraft-cli run --commands art.json --export art.vectorcraft --export art.svg --export art.png --scale 2`.
   `--export` runs after the steps and picks the format from the extension; `--scale` sizes raster exports (pixels per point; default 1).
   `--in FILE` opens an existing file first (any readable format). Each step prints one JSON line with its result.
3. **Look at the PNG** with the image viewer and fix what is wrong. Read `info art.vectorcraft` for artboards, object counts, fonts.
4. Commit the `.vectorcraft` source and the generator JSON with the exports.

Formats: readable `.vectorcraft .svg .pdf .ai .psd .png .jpg ... .dxf .eps`; writable `.vectorcraft .svg .svgz .pdf .ai .png .jpg .gif .webp .tif .bmp .tga .psd .eps .emf .wmf .dxf`.
`vectorcraft-cli convert IN OUT [--scale N] [--artboard I | --range R]` converts without steps (`--outline-text` writes SVG text as paths).

## Model (verified)

- Units are points; `file.new {name,width,height,units:"Pixels",backgroundContents:"transparent"|"white",colorMode:"rgb"}` makes a document with one artboard. For game art use Pixels and a power-of-two size.
- Object ids are sequential integers; the first object is `2`. **A later step can use `"$N.field"`** for step N's result (step 0 is the first step): `"ids": ["$1.id"]`.
- New objects are **selected**. `paint.setFill` / `paint.setStroke` / `stroke.set` apply to the **selection**. Draw, style, then draw the next. To style an older object: `select.set {"ids":[2]}` first.
- Shapes: `shape.rectangle {x,y,width,height,radius?,rotation?}`, `shape.ellipse`, `shape.polygon`, `shape.star {cx,cy,radius1,radius2,points,rotation?}`, `shape.line`, `shape.arc`, `shape.spiral`, grids; `path.create {anchors:[{x,y,in?,out?}],closed?} | {d: "SVG path data"}` for anything else. The y axis points down.
- `paint.setFill {color:"#rrggbb" | gradient:{kind:"linear|radial|freeform", angle, stops:[{offset,color}]}}`, `paint.setStroke {color}` then `stroke.set {weight, cap, join, align, dash}`. **The stroke width key is `weight`** (`width` is silently ignored).
  A linear gradient with `angle: 90` ran bottom to top (stop 0 at the bottom): check by rendering.
- Text: `text.create {x,y,text,size,font?,style?,color?,justify:"left|center|right"}`; for point type `x` is the alignment point. Check fonts with `text.fonts` (`status: exact|substitute|missing`); a missing font is replaced silently. Use `vectorcraft-cli convert ... --outline-text` for SVG that must not depend on fonts.
- Combine and edit: `object.pathfinder.unite|minusFront|intersect|exclude|divide|trim|merge|crop|outline`, `object.group`, `object.align`, `object.distribute`, `object.transform|move|rotate|scale|reflect`, `object.arrange.*`, `object.clippingMask.make`, `object.compoundPath.make`, `layer.*`, `effect.apply` (live effects such as glow), `symbol.*`, `pattern.*`, `brush.*`, `imageTrace.*` (raster to vector).
- Inspect: `document.inspect {depth?, kind?, ids?}` returns the layer tree (id, name, kind, bounds, fill, stroke). Use it to confirm a style landed.
- Several artboards (`artboard.new {x,y,width,height,name}`): `file.exportForScreens {folder, artboards|range|fullDocument, formats, preset, prefix}` exports every artboard, or `assets.add`/`assets.export` exports marked objects cropped to themselves (an icon sheet). A single `--export x.png` writes the first artboard.

## Recipe (verified): `reference/star-icon.json`

A 256x256 rounded-square badge with a two-stop gradient, a thick dark outline, a star and a label.

```
vectorcraft-cli run --commands star-icon.json --export star.vectorcraft --export star.svg --export star.png --scale 2
```

Pattern to copy: `file.new` -> shape -> `paint.setFill` -> `paint.setStroke` + `stroke.set weight` -> next shape -> ... -> `select.set` the shapes to restyle.

## Icon and sprite guidance for games

- Design on a pixel grid at the final size (64, 128, 256, 512). Keep a 8-16 px safe margin and a consistent stroke weight across a set.
- One dark outline color and one light highlight per set makes art read at small sizes; avoid thin strokes (under 2 px at 1x).
- Export PNG at 1x and 2x; keep SVG only if the project uses Unity's Vector Graphics package.
- A set of icons: one document, one artboard per icon (`artboard.new`), then `file.exportForScreens` with a `prefix`. Name artboards after the asset.
- Build repeatable sets from a script with a loop in PowerShell that writes the JSON (colors, labels as variables) rather than hand-editing each icon.

## Gotchas

- Silent no-ops: wrong param names are ignored (see `weight`). Render or `document.inspect` after styling.
- The step list is not atomic like FigCraft's; read the printed results for errors.
- Defaults add a thin black stroke to new shapes. Call `paint.setStroke`/`stroke.set` or `paint.none` for filled-only art.
- `info FILE` reports import warnings for foreign formats (.ai, .psd, .eps); use `--replace-lossy` only when overwriting is intended.
- The CLI has `bench` and `perf` for performance checks; irrelevant for asset work.
