# Dirty Flag

Use when derived state is expensive and inputs change less often than reads. This is an invalidation
mechanism, not a license to hide mutable dependencies.

- Enumerate every dependency and mark dirty through controlled mutations. Batch changes where
  possible; recompute lazily on access or once at a defined synchronization point.
- Use a generation/version counter when asynchronous results can complete out of order; publish
  a result only if it still corresponds to the current inputs.
- Test multiple mutations, repeated reads, external invalidation, and a cancelled computation.

Do not leave public mutable inputs that bypass invalidation, retain obsolete cached asset handles,
or assume Unity transforms automatically invalidate your own derived cache.