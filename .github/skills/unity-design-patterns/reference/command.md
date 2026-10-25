# Command

Use to represent an operation for queuing, history, undo/redo, replay, or deferred execution.

- Separate command data from execution where replay or serialization matters. Capture stable
  identifiers and required prior state rather than a mutable scene-object snapshot by accident.
- Define validation, success/failure, execution ordering, and whether undo is valid. Redo after a
  new command clears the obsolete history branch.
- Bound retained history and resources. Cancellation and partial execution need a recovery rule.
- Test deterministic operations, invalid targets, inverse correctness, and redo branching.

Do not imply every command is reversible. Network replay requires simulation ticks/authority and
determinism, not simply re-running an arbitrary delegate with live Unity references.