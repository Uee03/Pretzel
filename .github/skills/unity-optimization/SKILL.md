---
name: unity-optimization
description: "Profile and optimize Unity CPU, GPU, memory, GC, frame spikes, collections and ZLinq, UI, physics, rendering, and asset loading. Use for measured hot paths or performance investigations in 2D/3D games and reusable packages."
---

# Unity optimization

## Workflow

1. Identify target hardware, player build/backend, representative workload, frame and memory budgets.
   With no capture, return hypotheses and a capture plan, not asserted bottlenecks.
2. Reproduce on a development player using Profiler/markers as appropriate. Separate main-thread,
   render-thread, GPU, physics, managed allocation, and retained/native memory costs. Deep profiling
   changes timings; record its use and avoid comparing incompatible capture configurations.
3. Rank dominant costs by evidence. Look locally for frame-loop searches, repeated component access,
   closures/boxing, material instances, per-frame text/layout, expensive simulation, and leaked handles.
4. Choose a small intervention: cache stable references, bound work, eliminate unnecessary updates,
   reuse buffers, pool verified lifetimes, or improve content/shader cost. Account for invalidation.
5. For installed ZLinq, inspect its Unity instructions and namespaces. Use `AsValueEnumerable()` only
   on supported sources/operators and measure the whole expression. `ToArray`, materialization,
   captured predicates, or interface conversions can still allocate; do not rewrite cold-path LINQ.
6. Check Burst/Jobs suitability only for measured data-parallel work. Unity object access, managed
   references, delegates, and R3/UniTask APIs are not automatically Burst-compatible. No DOTS mandate.
7. Evaluate rendering for the selected pipeline: batching, instancing, draw calls, overdraw, variants,
   texture/mesh size, culling, and VFX capacity. Use Frame Debugger and GPU profiling, not API folklore.
8. Rerun identical player workloads. Record frame-time distribution/spikes, GC bytes, retained memory,
   and any regressions. Verify correctness and target builds before accepting the optimization.

## DO NOT

- Claim zero allocation because a library advertises it or claim speedup from source inspection.
- Treat GC alone as the performance budget or add an unbounded static cache to avoid allocation.
- Assume cached `WaitForSeconds` is valid for all concurrent/time-varying uses, or cache dynamic
  Unity references without an invalidation rule.
- Replace every collection/algorithm or change 2D/3D/pipeline/platform choices to optimize speculatively.

## Sources

- [Unity Profiler](https://docs.unity3d.com/Manual/Profiler.html)
- [ProfilerMarker](https://docs.unity3d.com/ScriptReference/Unity.Profiling.ProfilerMarker.html)
- [ZLinq Unity section](https://github.com/Cysharp/ZLinq#unity)
- [Pool lifetime](../unity-object-pooling/SKILL.md)
