---
name: craft-tools
description: "Use FigCraft, VectorCraft and SoundCraft (ArtCraft's local design, vector and audio apps) from the command line to make UI art, icons and sound effects for games. Covers where the CLIs are, how to drive them headlessly or through MCP, Windows PowerShell quoting pitfalls, file confinement, verification and how to deliver assets into a Unity project. Load this first, then figcraft-ui, vectorcraft-art or soundcraft-audio."
---

# Craft tools (FigCraft, VectorCraft, SoundCraft)

Local desktop apps from ArtCraft, each with a `*-cli.exe` that runs the same engine as the app **without a window**.
They are how an agent makes art and sound: write a command script, run it, look at the result, iterate.

| Tool | Makes | CLI (not on PATH) | Version seen | Skill |
|---|---|---|---|---|
| FigCraft | UI layouts, buttons, panels, mockups, frames with auto layout, components (Figma-like) | `C:\Program Files\FigCraft\figcraft-cli.exe` | 0.10.0 | `figcraft-ui` |
| VectorCraft | Icons, logos, illustrations, sprites sources (Illustrator-like) | `C:\Program Files\VectorCraft\vectorcraft-cli.exe` | 0.16.0 | `vectorcraft-art` |
| SoundCraft | Sound effects, jingles, music (a DAW with synths) | `C:\Program Files\SoundCraft\soundcraft-cli.exe` | 0.14.0 | `soundcraft-audio` |

PdfCraft is installed too (`C:\Program Files\PdfCraft\pdfcraft-cli.exe`); it is not covered here. The apps come from the ArtCraft
launcher; VectorCraft's `README.md` and `ASSETS.md` sit in its install folder. In PowerShell define
`$fig`, `$vec`, `$snd` as the full paths; do not assume PATH.

## How to drive them

1. **Headless batch (use this by default).** No app needs to run. Write a script file, run it, export, inspect.
   - FigCraft: `figcraft-cli run script.json [--in file.figcraft] [--out file.figcraft]`, then `render` or `export`.
   - VectorCraft: `vectorcraft-cli run [--in FILE] --commands script.json --export out.svg --export out.png [--scale 2]`.
   - SoundCraft: `soundcraft-cli script script.txt [--in s.scraft] [--bounce out.wav --start 0 --end 1] [--save s.scraft]`.
2. **MCP over stdio.** `figcraft-cli mcp`, `vectorcraft-cli mcp`, `soundcraft-cli mcp` serve the same commands as MCP tools
   (this is what an MCP-capable client attaches). Without an attach flag they run an in-process headless engine.
3. **Attach to a running app** (to see the user's open document): start the app with `--control PORT`, then
   `figcraft-cli mcp --attach PORT`, `vectorcraft-cli mcp --connect 127.0.0.1:PORT` (default 7979) or
   `soundcraft-cli mcp --connect PORT`. `soundcraft-cli app [--port P] <method> [json]` calls a running app directly.
   Only attach when the user asks to work in their open document; headless is repeatable and safe.

The chat tools of a session cannot load a new MCP server mid-session, so an agent normally uses mode 1 through the shell.

## The command model (all three)

- Every action is a **command id** with JSON params (`shape.rectangle`, `node.create`, `midi.note_add`, ...).
- Discover instead of guessing: `figcraft-cli commands --json [--filter x]`, `vectorcraft-cli commands`,
  `soundcraft-cli commands [filter]` and `soundcraft-cli describe <id>`. The catalogues are large (FigCraft 569 commands,
  VectorCraft 802, SoundCraft 482). Dump one to a temp file and filter it with PowerShell `ConvertFrom-Json`; never paste a whole catalogue into chat.
- Each `params` string in a catalogue shows the exact keys. A command's result (often `{id}`) is printed as one JSON line per step.
- Ids are **sequential integers**, and a fresh document starts at 2 (the first created object is `2`). Steps can often be
  predicted, but read the printed result when in doubt.
- **Step references:** VectorCraft accepts `"$1.id"` (result field `id` of step 1, step 0 is the first step) in a later step's params.
  FigCraft does **not** (it rejected `"$1.id"`): create in one run, then continue with `--in`/`--out` and the printed id, or predict the id.

## Rules that save time (learned by running them)

- **Use script files, not inline JSON.** Windows PowerShell 5.1 strips the double quotes inside arguments passed to a native
  program, so `--cmd 'x={"a":1}'` fails with "key must be a string". Write the JSON to a file (use the file-creation tool, not a
  here-string redirect) and pass the path. PowerShell 7 does not have this problem, but do not depend on it.
- **stderr noise is not failure.** The CLIs print status ("wrote ...") to stderr, which PowerShell shows as an error record.
  Capture with `2>&1 | Out-String` and check `$LASTEXITCODE`.
- **Unknown or misspelled params may be ignored silently** (VectorCraft `stroke.set {"width": 6}` returned success and did
  nothing; the key is `weight`). Always verify by rendering or inspecting, never trust "ok".
- **FigCraft `run` is atomic:** if any step fails, the whole run is rolled back and no `--out` file is written.
- **Verify visually.** Render to PNG and open it with the image viewer tool (view the PNG) before reporting. For sound,
  check `soundcraft-cli info out.wav --compact` (peak, loudness) and, if there is a way to listen, say it was not listened to.
- **Determinism:** FigCraft takes `--no-system-fonts` (built-in fonts only, same render on every machine). Use it for assets that are committed.
- **Confine writes** when running something you did not write: `--automation-read-root DIR` / `--automation-write-root DIR`
  (FigCraft `run/export/render/info`, VectorCraft `mcp`, SoundCraft `mcp`). Give no write root and writing is denied. Prefer them for MCP.
- Do not overwrite the user's source files. SoundCraft `--in X --save X` needs `--in-place`; keep it that way.

## Project conventions for game assets

Keep the editable **source** next to the **exports**, and commit both, so assets can be regenerated.

```
<project>/Art/Source/      *.vectorcraft  *.figcraft  *.scraft  and the generator scripts (*.json, *.txt)
<project>/Assets/...       exported *.png  *.svg  *.wav   (what Unity imports)
```

For a UPM package or sample, keep source under `Art~/` (hidden from Unity) and exports inside the sample folder.
Write a short `README` line per asset set saying which script produced it. A re-runnable script beats a one-off GUI edit.

## Delivering into Unity (Unity 6000.0+)

- **PNG** from VectorCraft/FigCraft: export at 1x and 2x (`--scale 2`), power-of-two canvas sizes for atlases and icons (64, 128, 256, 512).
  Set Texture Type = Sprite (2D and UI), disable mipmaps for UI, keep `Alpha Is Transparency` on. UI Toolkit takes PNG backgrounds; for 9-slice, set the slice in USS (`-unity-slice-*`), not in the image.
- **SVG** needs Unity's Vector Graphics package; do not assume it is installed. Prefer PNG for UI Toolkit and sprites unless the project already uses SVG.
- **WAV** (16-bit, 44.1 kHz) for short effects: import with Load Type = Decompress On Load, Compression = PCM or ADPCM, Force To Mono for 2D sounds.
  Longer music: Compressed In Memory or Streaming, Vorbis. SoundCraft writes `wav`, `aiff` and `flac`; `convert` sets bit depth, rate and normalizes.
- Never create `.meta` files by hand. Let the Editor import new files, or copy them into the project's `Assets` folder and let Unity generate the metas.

## Safety and honesty

- The apps and their bundled fonts and assets have their own licenses (VectorCraft: MIT OR Apache-2.0 for the code; see its `ASSETS.md` and `OFL-*.txt` for fonts and assets). Check `ASSETS.md` before shipping text rendered with a bundled font.
- Generated sound is **not auditioned** by the agent. Report numbers (length, peak, loudness) and say it needs a listen.
- Say what was verified (rendered and viewed) and what was not.
