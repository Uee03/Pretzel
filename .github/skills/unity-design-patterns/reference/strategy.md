# Strategy

Use to substitute a cohesive algorithm: movement, scoring, targeting, loading, or serialization.

- Define a small contract with explicit inputs, results, and ownership. Inject the strategy into
  the consumer; do not have it query global managers for hidden state.
- Stateless ordinary types are usually enough. ScriptableObjects can author strategies but
  shared mutable execution state must live outside the asset or in an owned runtime instance.
- Choose supported serialized polymorphism or a factory at the Unity boundary; an interface
  field alone does not provide Inspector serialization.
- Test every implementation against the same contract, including invalid/default inputs.

Prefer a function/delegate for a single stateless operation when a family of classes adds no value.