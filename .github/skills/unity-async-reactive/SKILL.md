---
name: unity-async-reactive
description: "Choose and implement Unity async/await, UniTask, coroutines, R3 observables, C# delegates, Action, Func, and LitMotion tweens. Use for cancellation, subscription lifetime, PlayerLoop timing, pooled callbacks, or async exception handling."
---

# Unity async and reactive ownership

## Choose the mechanism

| Need                                       | Preferred mechanism when supported                                      |
| ------------------------------------------ | ----------------------------------------------------------------------- |
| One synchronous callback or transformation | Typed delegate, Action, or Func with an explicit owner                  |
| One asynchronous result/workflow           | Installed UniTask; preserve existing Task/Awaitable contracts as needed |
| Ongoing composed event/state stream        | Installed R3 with explicitly owned subscriptions                        |
| Simple sequential frame behavior           | Coroutine if cancellation/result composition is not needed              |
| Interpolation or UI/gameplay tween         | Installed LitMotion; cancel by the actual owner lifetime                |

## Workflow

1. Verify installed package/version/integration assemblies; do not copy UniRx operators into R3
   or DOTween APIs into LitMotion. Consult [package details](./reference/package-lifetimes.md).
2. Define scene, component, enable-cycle, pool-lease, request, or application lifetime. Cache a
   supported destruction token while the component is alive; use a separate token for narrower scopes.
3. Return awaitable results rather than `async void`/`UniTaskVoid` for workflows consumers must await.
   Propagate cancellation to leaves. A `.Forget()` boundary needs a deliberate exception policy.
4. Specify PlayerLoop timing, scaled/unscaled time, main-thread access, and overlap policy (replace,
   queue, reject, or parallel). Async does not imply another thread or a hitch-free operation.
5. Own/unsubscribe C# events and dispose R3 subscriptions. Store removable delegates; a new lambda
   is not the same handler. Action models no result; Func models a result; use typed args deliberately.
6. Coroutine cancellation must cover component/GameObject state and explicit stop behavior. Coroutine
   work runs on the main thread; do not rely on yields to hide expensive synchronous operations.
7. Cancel/reset motion on lease return or view detachment, not only object destruction. Treat awaited
   motion cancellation and completion callbacks as distinct behavior; test the installed overloads.
8. Test interruption, disable/enable, scene unload, return/re-rent, reentrancy, and exception reporting.

## DO NOT

- Await the same ordinary UniTask repeatedly or concurrently unless its documented sharing mechanism
  is used. Do not assume UniTask or Unity Awaitable has Task's consumption semantics.
- Use `.Result`, `.Wait()`, unowned subscriptions, `async void` helpers, or silently swallowed exceptions.
- Access Unity objects from worker threads, mutate released pooled objects, or equate cancellation of
  an await with cancellation/cleanup of the underlying Unity load operation.

## Sources

- [UniTask](https://github.com/Cysharp/UniTask)
- [R3 Unity section](https://github.com/Cysharp/R3#unity)
- [LitMotion](https://github.com/annulusgames/LitMotion)
- [Coroutines](https://docs.unity3d.com/Manual/Coroutines.html)
