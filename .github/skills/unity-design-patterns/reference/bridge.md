# Bridge

Use when two dimensions vary independently, such as an ability family and its presentation backend.
Strategy changes an algorithm; Bridge separates two evolving hierarchies.

- Compose the abstraction with an implementation contract instead of multiplying subclasses for
  every combination. Keep Unity/pipeline/platform details in implementation adapters.
- State who creates, replaces, and disposes each implementation. Verify compatibility across
  combinations rather than testing only one happy-path backend.
- For packages, optional backends must not become unconditional assembly dependencies.

Do not add Bridge for one implementation or one fixed variation. Avoid generic hierarchies that
obscure the actual operations the abstraction needs.