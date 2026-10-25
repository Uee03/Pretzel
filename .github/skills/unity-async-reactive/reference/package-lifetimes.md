# Package integration and lifetimes

## UniTask

Use version-matched PlayerLoop timing and cancellation overloads. UniTask supports lifetime tokens
such as `destroyCancellationToken` on compatible Unity versions and its own destruction helpers on
older versions. Destruction is too late for a pool return or state exit; cancel at that boundary.
Ordinary UniTask values are not reusable Task promises. Check documented preservation/sharing
options for multiple consumers. Prefer awaitable `UniTask` over `UniTaskVoid` for composed workflows.
Cancellation of an adapter may only cancel waiting, so inspect operation ownership separately.

## R3

The core NuGet package and Unity integration are distinct installation facts. Verify both before
using Unity-specific triggers, frame/time providers, or lifecycle extension methods. Inspect R3's
documented error/completion model; UniRx/System.Reactive signatures are not interchangeable.
The subscribing scope owns disposal. Where available, destruction-bound `AddTo` helpers do not
replace enable-cycle or pool-lease disposal. Rebinding must dispose old subscriptions first.

## LitMotion

Inspect the installed builder/binding APIs and integration asmdefs. Retain the motion handle when
explicit cancellation is needed. Upstream supports cancellation, destruction-bound `AddTo`, and
UniTask integration, but the exact overloads depend on the installed version. Cancel a pooled
object's motion on return and reset all properties changed by the tween. Capturing an object in
a binding may retain it; check callback allocation and lifetime in a player workload.

## Delegates and coroutines

Use a strongly typed delegate for a meaningful contract, Action for notification, and Func for
calculation/results. Clarify sync/async result semantics and listener exceptions. Keep handles to
unsubscribe and avoid captured mutable loop/lease state. Starting a coroutine is not starting a
thread; synchronous sections still block the frame. Choose yields/time domains deliberately.

Sources: [UniTask README](https://github.com/Cysharp/UniTask),
[R3 README](https://github.com/Cysharp/R3), [LitMotion README](https://github.com/annulusgames/LitMotion).