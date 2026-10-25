# MVP

Use when a passive view exposes events and rendering operations while a presenter controls UI flow.

- The model owns rules; the presenter translates model state into view updates and user intent
  into model commands. A UI Toolkit adapter holds VisualElement/UXML details.
- Prefer a narrow view contract and an ordinary C# presenter when useful for isolated tests.
- Bind on the actual view lifetime; unbind callbacks and dispose R3 subscriptions on teardown.
  Reset recycled ListView rows and avoid binding the same presenter repeatedly.
- Test presenter behavior with a fake view, then test focus, input, and layout in Unity.

Do not put gameplay rules in click handlers or require a presenter interface solely for naming
symmetry. An existing uGUI base is not automatically a UI Toolkit abstraction.