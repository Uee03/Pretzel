# Observer

Use for one producer with independently owned consumers. Choose C# events for small synchronous
notifications, UnityEvent for Inspector-authored wiring, or installed R3 for stream composition.

- Define ordering, replay/initial value, error, and reentrancy behavior. Expose observation without
  granting consumers mutation rights when supported by the actual API.
- The subscribing view/component/lease owns unsubscription or disposal. Destruction lifetime
  does not cover disable/enable or pool return; use the narrower scope.
- Check project observables before reuse: equality, persistent vs runtime listeners, allocation,
  and error behavior differ. An existing helper is not automatically interchangeable with R3.
- Test producer absence, repeated binding, listener exceptions, and owner teardown.

Avoid static global streams for scene-local state, duplicated listeners after enable cycles, and
retaining a destroyed Unity object through a captured callback.