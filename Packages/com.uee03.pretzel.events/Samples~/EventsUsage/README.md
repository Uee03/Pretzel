# Events Usage

| File | What it shows |
|------|---------------|
| `EventsUsageExample.cs` | An `EventBus` with a typed struct message, a subscription disposed in `OnDisable`, and an optional `IntEventChannel` raised with the score. |
| `EnemyDefeated.cs` | The message: a small readonly struct. |

## Try it

1. Add `EventsUsageExample` to a GameObject and press Play.
2. Use the component's context menu (**Defeat Slime**, **Defeat Dragon**) to publish messages.
3. Optional: create **Assets > Create > Pretzel > Events > Int Channel**, assign it, then add an **Int Event Listener**
   to another object with the same channel and wire its response to, for example, a `Text` field.

Rules:

- `EventBus` is an ordinary object. Own one per game or scope and pass it in; do not make it a static global.
- Always dispose subscriptions when the subscriber is disabled. Channels are assets and outlive scenes.
- A handler that throws is logged and the other handlers still run.
