---
description: 'Use for explicitly requested standalone .NET code. Unity package code follows unity-csharp.instructions.md instead.'
---

# C# Coding Conventions

This instruction is on-demand, not an automatic rule for Pretzel package files. For Unity projects
and UPM packages, prefer `unity-csharp.instructions.md`: preserve the supported Unity C# level,
Unity object/main-thread lifetimes, and ordinal case-sensitive path semantics. The .NET-specific
rules below do not require ASP.NET, desktop UI, or .NET solution conventions in Unity.

- **Explicit types over `var`:** write `List<Customer> customers = ...`, not `var customers`.
- **Braces on all control blocks**, even single-line `if`/`for`/`while`.
- **`async`/`await` all the way down** for IO; suffix async methods with `Async`.
- **Case-insensitive string comparison by default:** `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)`
  and `StringComparison.OrdinalIgnoreCase` in `Contains`/`StartsWith`/dictionary keys. Never `==` when
  case should be ignored.
- **One public type per file;** file name matches the type name.
- Use `readonly` fields and immutable models (`record`) only where value/immutable semantics genuinely
  help; **classes by default**. Avoid `struct`/`record struct` without a demonstrated need.
- Prefer `IReadOnlyList<T>` / `IReadOnlyDictionary<T>` on public APIs over mutable collections.
- **Prefer composition over inheritance.** No empty marker base classes; use small interfaces where
  they add value.
- **Services go behind an interface.** Any injected service that has behavior or crosses a boundary
  (data access, external APIs, email/storage, domain/application services) is defined as `IFooService`
  + `FooService` and registered by its interface. This keeps them mockable in tests and swappable
  (Strategy/Factory). Don't wrap DTOs, records, value objects, `IOptions<T>` config, or ViewModels in
  interfaces just to have one.
- **Prefer explicit/manual mapping** (extension methods or constructors) over AutoMapper/Mapster.
- **CQRS is a pattern, not a library.** Where CQRS applies, use your own `ICommand`/`IQuery` + handler
  interfaces and decorators; **do not add MediatR** to a new project. Inject the specific handler and
  call it directly — no `ISender`/mediator. Keep queries free of domain logic; don't apply CQRS to a
  simple CRUD domain.
- **Constructor injection only** — no service locator, no `new`-ing injectable dependencies.
- LINQ only where it improves clarity; prefer explicit `foreach` for hot paths / simple loops.
- No `Console.WriteLine` for diagnostics in library code — use `Microsoft.Extensions.Logging`.
- Never hardcode secrets; never string-concatenate untrusted input into SQL (always parameterize).
