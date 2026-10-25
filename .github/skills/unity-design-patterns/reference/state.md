# State

Use for behavior that depends on a finite state and has explicit transition rules.

- A small enum plus transition table can be enough; use state objects when behavior genuinely varies.
- Define Enter/Exit/Update contracts, allowed transitions, and guards. Transition in one owner;
  specify how nested transitions and asynchronous completion are serialized.
- Each state owns its subscriptions, async work, and temporary resources. Exit cancels/disposes
  them; a late callback must not mutate the next state's context.
- Test invalid transitions, reentry, interruption, and owner destruction or pool return.

Do not combine independent state dimensions into an exploding set of subclasses or assume a network
client may authoritatively advance server-owned states.