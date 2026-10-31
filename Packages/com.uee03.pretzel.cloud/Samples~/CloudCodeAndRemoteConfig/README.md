# Cloud Code And Remote Config

| File | What it shows |
|------|---------------|
| `CloudCodeAndRemoteConfigExample.cs` | Sign in, fetch Remote Config with user attributes, read typed values with defaults, and call a Cloud Code module endpoint with a typed request and response. |
| `GreetingRequest.cs`, `GreetingResponse.cs` | The typed request and response of the example endpoint. |
| `GameTuning.cs` | A JSON Remote Config value read with `GetJson<T>`. |

This sample compiles only when the **Authentication**, **Cloud Code** and **Remote Config** packages are installed
(its asmdef has the same version defines and define constraints as the adapter assemblies).

## What you need to create in your project

Nothing here is deployed for you. The sample calls these names, which you can change on the component:

- A Cloud Code **module** named `HelloWorld` with an endpoint `SayHello` that takes a `name` parameter and returns an
  object with a `message` field. Write and deploy it with the Cloud Code module tooling for your version (see Unity's
  Cloud Code documentation); this package does not ship server code and the endpoint is not verified here.
- Remote Config keys `welcome_message` (string) and `game_tuning` (JSON such as `{"enemySpeed":1.5,"maxEnemies":20}`).

Until you create them the sample still runs: Remote Config getters return their defaults and the module call reports
`NotFound` as a result, not an exception.

## Try it

1. Link the project and enable Cloud Code and Remote Config in the Unity Dashboard (see the package README).
2. Add `CloudCodeAndRemoteConfigExample` to a GameObject and press Play. Results go to the Console.

Notes:

- The request type's fields are the endpoint's parameter names; the response type matches the endpoint's JSON result.
  Both are converted with the `ISerializer` given to `CloudServices.CreateBuilder` (Newtonsoft by default).
- A failed Remote Config network fetch keeps the cached values and still succeeds; check `IsFresh`.
- Branch on `CloudErrorKind`, never on SDK exception types: the samples never reference Unity Services types.
