---
name: unity-design-patterns
description: "Choose and apply Unity design patterns: Observer, Strategy, Bridge, Singleton, State, Command, Object Pooling, Flyweight, Dirty Flag, MVP, MVVM, and Chain of Responsibility. Use for concrete variation, ownership, and testability problems, not pattern-driven rewrites."
---

# Unity design patterns

## Workflow

1. State the current behavior, actual variation point, lifetime, and measurable pain. Check nearby
   project helpers before adding a pattern. Compare a direct implementation as the baseline.
2. Select the smallest pattern that isolates that variation; load only its reference below.
3. Define participants, ownership, invariants, and failure cases in the existing project layout.
   Keep Unity adapters separate from portable rules where useful; do not force every type abstract.
4. Implement a thin slice and test substitution, cancellation/disposal, and lifecycle boundaries.
5. Explain why the pattern earns its complexity and when the simpler baseline remains preferable.

| Need                                              | Reference                                                         |
| ------------------------------------------------- | ----------------------------------------------------------------- |
| Notify independent consumers                      | [Observer](./reference/observer.md)                               |
| Swap one algorithm                                | [Strategy](./reference/strategy.md)                               |
| Vary abstraction and implementation independently | [Bridge](./reference/bridge.md)                                   |
| One explicitly owned process/session service      | [Singleton](./reference/singleton.md)                             |
| State-specific behavior and transitions           | [State](./reference/state.md)                                     |
| Queue, replay, or undo an operation               | [Command](./reference/command.md)                                 |
| Reuse expensive short-lived instances             | [Object Pooling](../unity-object-pooling/SKILL.md)                |
| Share immutable intrinsic data                    | [Flyweight](./reference/flyweight.md)                             |
| Recompute only invalidated derived values         | [Dirty Flag](./reference/dirty-flag.md)                           |
| Presenter controls a passive view                 | [MVP](./reference/mvp.md)                                         |
| View binds observable state and commands          | [MVVM](./reference/mvvm.md)                                       |
| Ordered handlers may accept or forward            | [Chain of Responsibility](./reference/chain-of-responsibility.md) |

## DO NOT

- Apply every named pattern to every feature or replace working code merely to match a diagram.
- Use a global event bus or Singleton to hide dependency and lifetime decisions.
- Mistake a generic interface for a useful abstraction or data sharing for object pooling.
- Force MVP/MVVM without considering UI Toolkit binding capabilities in the installed version.

## Sources

- [Unity learning resources](https://learn.unity.com/)
- [R3](https://github.com/Cysharp/R3)
- [Unity ObjectPool](https://docs.unity3d.com/ScriptReference/Pool.ObjectPool_1.html)
