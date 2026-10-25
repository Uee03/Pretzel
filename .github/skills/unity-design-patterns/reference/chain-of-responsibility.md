# Chain of Responsibility

Use for an ordered set of handlers that can accept, reject, or forward a request: interaction
resolution, input routing, validation, or an ability execution pipeline.

- Make ordering deterministic and terminal outcomes explicit: handled, not handled, rejected,
  or failed. Decide whether exactly one handler or multiple handlers may act.
- Prefer a simple collection of handlers over mutable linked nodes when it improves visibility.
- For async handlers, propagate cancellation and stop forwarding after a terminal outcome.
  Side effects need a compensation policy if later stages can fail.
- Test no match, first match, rejection, exception, cancellation, and order-dependent behavior.

Do not hide an unbounded traversal, a cycle, or repeated side effects behind the chain. Distinguish
single-consumer resolution from broadcast Observer behavior.