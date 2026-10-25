---
name: dotnet-clean-architecture
description: 'Designs clean, layered .NET/C# solution structure — project layout, dependency direction, DI, SOLID, and where code belongs. Use when creating a new .NET solution, adding a project/layer, wiring dependency injection, or deciding which layer a class goes in. Applies to API, web app, desktop, and cross-platform .NET apps.'
---

# .NET Clean Architecture

## Unity/UPM suitability

For Pretzel packages, use this as an optional source of layering and SOLID ideas, not as a prescribed
solution layout or framework mandate. Follow `AGENTS.md` and `unity-csharp.instructions.md` for Unity
compatibility, package boundaries, assemblies, serialization, and lifecycle. Unity packages do not
need ASP.NET, desktop, `Microsoft.Extensions.DependencyInjection`, `IOptions`, or the .NET app layouts
below. Add interfaces or separate assemblies only for demonstrated boundaries. Keep path and package
identifier comparisons case-sensitive where their domain requires it.

These are foundation rules for separately requested .NET apps. Web, desktop, and cross-platform
scaffolding guidance is intentionally not included in this Unity repository.

## Core principle

Dependencies point **inward**. The domain knows nothing about frameworks, UI, or infrastructure.
Outer layers depend on inner-layer **interfaces**, never the reverse.

## Layout — general apps (desktop / CLI / cross-platform)

```
src/
  <Project>.Core/           domain models, interfaces, business logic (NO UI, NO framework deps)
  <Project>.Infrastructure/ implementations: data access, external APIs, file IO, DI wiring
  <Project>.App/            entry point / UI layer (thin)
tests/
  <Project>.Tests/          unit tests (xUnit)
```

## Layout — API / SaaS backend

```
src/
  <Project>.Api            HTTP host: endpoints/controllers, middleware, DI, OpenAPI
  <Project>.Contracts      public API model: Requests/Responses/DTOs (NO Domain reference)
  <Project>.Application    use cases (Commands/Queries), validators, interfaces, orchestration
  <Project>.Domain         entities, value objects, enums, business rules (framework-free)
  <Project>.Persistence    DB: Npgsql/Dapper/EF Core, repositories, migrations, SQL
  <Project>.Infrastructure external concerns: email, storage, PDF, payment, 3rd-party
  <Project>.SharedKernel   cross-cutting primitives (Result, guards)
tests/
  <Project>.UnitTests / .IntegrationTests / .ArchitectureTests
```

Dependency direction: `Api → Application → Domain`, `Api → Contracts`; `Infrastructure`/`Persistence`
`→ Application → Domain`. **Domain references nothing framework-related.** **Contracts never reference Domain.**

## SOLID (apply pragmatically)

- **S** one reason to change per class. **O** extend via new types/strategies. **L** subtypes honor
  the base contract. **I** small focused interfaces (`IReadStore`, `IWriteStore`). **D** high-level
  modules depend on abstractions.
- Prefer **composition over inheritance**. Don't create empty marker base classes; use small
  interfaces (`IEntity<TId>`, `IAuditableEntity`) only where they add value.

## Dependency injection

- `Microsoft.Extensions.DependencyInjection` (via `Microsoft.Extensions.Hosting` where a host fits).
- Register in one composition root, e.g. `ServiceCollectionExtensions.AddAppServices()`.
- **Define services behind an interface.** Every injected service with behavior or that crosses a
  boundary gets an `IFooService` abstraction and its `FooService` implementation, registered by the
  interface (`services.AddScoped<IFooService, FooService>()`). Consumers depend on `IFooService`, not
  the concrete type. This makes services mockable in unit tests (NSubstitute/Moq) and lets you swap
  implementations for **Strategy**/**Factory** without touching callers. Skip interfaces for DTOs,
  records, value objects, `IOptions<T>` config, and ViewModels — they carry data, not behavior.
- **Constructor injection only.** No service locator, no `new`-ing dependencies inside classes.
- Scoped/transient for stateful services; singletons for stateless/shared ones. Use DI where it adds
  value — not for trivial value types or one-off helpers.

## Patterns (use where they genuinely help)

Repository (data access) · Factory/Abstract Factory (provider creation) · Strategy (interchangeable
algorithms) · Options (`IOptions<T>` config) · **CQRS** (separate Command/Query models) when the app is
large enough. CQRS is a **pattern, not a library** — use your own `ICommand`/`IQuery` + handler
interfaces and decorators, **not MediatR**. See the [dotnet-cqrs](../dotnet-cqrs/SKILL.md) skill.

## Where code goes — quick decisions

- Pure business rule / entity behavior → **Domain/Core**.
- Orchestrating a use case (command/query) → **Application** (API) or a service in **Core**.
- Talking to DB / HTTP / files / SMTP → **Infrastructure**/**Persistence**, behind an interface
  declared in Application/Core.
- HTTP shape (request/response DTO) → **Contracts**. UI state (ViewModel/Presenter) → the UI project.

## Cross-cutting rules

- Config via `appsettings.json` + env vars (`Microsoft.Extensions.Configuration`); secrets from env /
  user-secrets — never hardcoded.
- Logging via `Microsoft.Extensions.Logging`; no `Console.WriteLine` for diagnostics in libraries.
- Testing: **xUnit** + **FluentAssertions** + **NSubstitute**; add **NetArchTest** architecture tests
  to enforce dependency rules on larger solutions.

## Coding conventions

See [.github/instructions/dotnet-conventions.instructions.md](../../instructions/dotnet-conventions.instructions.md)
for the enforced coding style (explicit types, braces, async, string comparison, mapping, records).
