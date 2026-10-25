---
name: unity-shader-graph
description: "Design and optimize Unity Shader Graph properties, Custom Function HLSL, materials, variants, batching, instancing, and pipeline-compatible shaders. Use for 2D/3D rendering or reusable shader packages without assuming URP/HDRP or graph automation."
---

# Unity Shader Graph

## Workflow

1. Identify pipeline, Shader Graph version, target platforms, graph target/subtarget, geometry,
   pass requirements, and visual acceptance criteria. Preserve the project's rendering choices.
2. Inspect existing graphs/materials and exposed reference names through supported tools/source.
   Specify the graph's inputs, outputs, coordinate spaces, color space, and precision requirements.
3. Prefer a small graph/subgraph change. For Custom Function HLSL, match documented signatures,
   precision suffixes, include scope, and target compatibility; do not assume pipeline includes
   are shared across URP/HDRP or compute/render contexts.
4. Define code/material ownership. Cache property IDs; avoid accidental per-renderer material
   cloning from `.material`. When considering MaterialPropertyBlock, check SRP Batcher and GPU
   instancing compatibility for the selected pipeline rather than assuming batching improves.
5. Audit overdraw, transparency, sample count, branches, variants/keywords, interpolators, and
   mobile precision. Measure GPU/frame-debugger evidence; fewer nodes does not prove lower GPU cost.
6. Graph editing requires the actual installed public Editor API or an approved available external
   tool. Without it, supply precise node/property/manual steps and mark graph changes unimplemented.
   Never invent API calls or rewrite graph YAML/JSON GUIDs as a shortcut.
7. Verify graph/shader import and compile, representative materials, supported render passes,
   player variants/stripping, and visual results on target hardware. Package assets must not depend
   on game-only textures or an undeclared pipeline package.

## DO NOT

- Treat a shader compiling in Editor as proof required variants survive a player build.
- Change pipeline/subtarget or add a render package without approval.
- Assume property blocks, transparent rendering, or reduced precision always improve performance.
- Declare a graph visually correct without viewing its output in the target pipeline.

## Sources

- [Shader Graph documentation - choose installed version](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/)
- [MaterialPropertyBlock](https://docs.unity3d.com/ScriptReference/MaterialPropertyBlock.html)
- [Shader.PropertyToID](https://docs.unity3d.com/ScriptReference/Shader.PropertyToID.html)
- [Profiling workflow](../unity-optimization/SKILL.md)
