---
name: unity-common-types
description: "Design Unity common classes, enums, Flags, constants, extensions, helpers, serializable models, and public package types. Use for shared-type placement, value/reference semantics, stable serialized identities, or utility reuse."
---

# Unity common types

## Workflow

1. Start from consumers and the nearest existing helper. Keep feature-specific types with their
   feature; put code in Common only when it is genuinely shared, not merely short or static.
2. Reuse compatible project libraries (including LazyJedi if present), but inspect semantics,
   allocation, public accessibility, and platform assumptions before making them dependencies.
3. Choose enums for closed choices. Assign stable explicit values for serialized/network/save data;
   do not reorder/reuse meanings. Flags need powers of two, a zero None value, and defined validation.
   Check the serializer/network format before changing an enum backing type.
4. Use const for true compile-time values and static readonly when runtime construction or public
   versioning warrants it. Public const values are compiled into consumers. Match existing conventions
   rather than making all shared settings global mutable state.
5. Favor small value types for genuine values with coherent equality and defaults; classes for
   identity/shared mutable state. Consider boxing/copy cost and Unity authoring serialization separately.
6. Extensions should be discoverable, narrow, and free of hidden object creation/resource ownership.
   Helpers should not hide service locators, scene searches, or cross-feature dependency cycles.
7. For packages, keep namespaces and public contracts stable and avoid leaking game enum/constants.
   Add interfaces only where a substitution seam is real; immutable DTOs do not each need one.
8. Test invalid enum inputs, serialized round-trip, equality/default behavior, and downstream compile
   compatibility when a public/shared type changes.

## DO NOT

- Make a giant Common/Utils dumping ground, duplicate an existing helper, or add arbitrary abstractions.
- Use enum display labels or array position as persistent identity without a deliberate format.
- Cache arbitrary caller inputs forever in a static dictionary to claim zero allocation.
- Silently rename serialized/public types or assume Unity can author every valid C# value type.

## Sources

- [Unity serialization](https://docs.unity3d.com/Manual/script-serialization.html)
- [C# enums](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/enum)
- [Package boundaries](../unity-clean-architecture/reference/package-boundaries.md)
