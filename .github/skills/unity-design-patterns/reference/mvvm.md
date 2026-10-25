# MVVM

Use when a view can bind declarative state and commands without a presenter coordinating each update.

- Keep the view-model independent of VisualElement and MonoBehaviour where feasible; define
  observable state, validation, and command availability with explicit ownership.
- Inspect the installed UI Toolkit runtime binding API before selecting it. Use R3 adapters if
  appropriate and available; R3 alone does not provide all WPF-style binding capabilities.
- Specify one-way/two-way synchronization, initial values, and feedback-loop prevention.
- Test view-model state without Unity and integration binding across detach/reattach in Unity.

Prefer MVP/manual updates for a small screen or unsupported binding features. Do not invent a
universal Unity ICommand/binding API or expose mutable domain objects directly as view-model state.