# Reusable package boundaries

- Confirm the package's `name`, semantic version, `unity` minimum, supported targets, dependencies,
  and distribution channel. A UPM package is not a Unity project and may need a consumer test host.
- Use Runtime and Editor asmdefs with explicit references; exclude Editor code from players.
  Core runtime must not depend on `Assets/_Scripts`, a bootstrap scene, or the author's game namespace.
- Put optional integrations in separate assemblies with verified version defines/constraints.
  Check behavior with the integration absent as well as present; conditionally compiling a class
  is insufficient if its asmdef still requires a missing reference.
- Public contracts should state ownership, disposal, thread/main-thread requirements, callback
  ordering, cancellation, and error semantics. Do not expose UniTask/R3 types unintentionally.
- Treat public/serialized type names and namespace changes as compatibility decisions. Plan
  migrations, semantic-version changes, and consumer updates rather than silently breaking assets.
- If requested, use `Samples~`, documentation, changelog, and package tests per UPM conventions.
  Do not generate these artifacts for every code change.
- Test the minimum supported Unity version and current supported host; include IL2CPP/stripping
  checks for generics, reflection, and NuGet dependencies when relevant.

Source: [Unity package layout](https://docs.unity3d.com/Manual/cus-layout.html).