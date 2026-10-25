---
name: dotnet-cqrs
description: 'Applies the CQRS pattern in .NET the right way — separate Command (write) and Query (read) models with lightweight custom abstractions (ICommand/IQuery + handlers + decorators), NOT a mediator library. Use when adding commands/queries, structuring the Application layer, deciding whether CQRS is warranted, considering MediatR, or separating read/write models/stores.'
---

# CQRS in .NET (without MediatR)

## Unity/UPM suitability

For Pretzel packages, CQRS is an optional design concept, not a default structure. Prefer the
existing package boundaries and ordinary direct APIs unless the domain demonstrates distinct command
and query use cases. Do not add handlers, dispatch infrastructure, DI frameworks, records, async
APIs, assemblies, databases, or message systems just to match the .NET examples below. Check the
package's minimum Unity/C# version and apply `unity-csharp.instructions.md`; do not impose web/API
endpoint, EF Core, or desktop app assumptions. This skill remains directly applicable to separately
requested standalone .NET applications.

CQRS (Command Query Responsibility Segregation) splits a system into a **write model** (commands that
change state) and a **read model** (queries that return data). Load
[dotnet-clean-architecture](../dotnet-clean-architecture/SKILL.md) first — CQRS lives in the
**Application** layer, organised by feature/vertical slice.

## The one rule that matters

**CQRS is a pattern, not a library. You do not need MediatR.** MediatR became synonymous with CQRS in
.NET, but they are not the same thing, and MediatR is now moving to a commercial license. What most
projects actually use it for — a thin dispatch layer for commands and queries — is a handful of
interfaces you own. Owning them gives you explicit dispatch (no `ISender`/runtime magic), simpler
debugging and onboarding, cleaner DI, and better testability.

Do **not** add MediatR to a new project. If an existing project already uses it and works, that's fine
— don't rip it out without a reason.

## When to use CQRS (and when not to)

**Use it when:** the domain has real business logic; task-based UIs (intent like "Book room", not
"set status = reserved"); collaborative/high-concurrency data; read load far exceeds writes and the
two need to scale/optimise independently; separate teams own reads vs writes; the system integrates
with event-driven subsystems.

**Skip it (plain CRUD is better) when:** the domain and rules are simple and a straightforward
create/read/update/delete data-access layer is sufficient. Don't add CQRS ceremony to a CRUD app.

Start at the simplest level and only move outward when a concrete need appears (see *Read/write model
separation* below). CQRS does **not** require separate databases, messaging, or Event Sourcing.

## Commands vs queries

- **Command** — changes state. Represents a business task ("CompleteTodo", "BookRoom"), not a raw data
  update. Carries validation + domain logic; may return nothing or a small result (e.g. the new id).
  Immutable value object (a `record`), just data + no behaviour.
- **Query** — returns data, **never** changes state. Returns a DTO/projection shaped for the caller,
  with **no domain logic**. Read directly from the store (Dapper/EF projection) — bypassing the
  domain model is intentional and fine.

## The abstractions (copy these, own them)

Put the contracts in the Application layer (or SharedKernel). Return types use the `Result` wrapper
from the SharedKernel for explicit success/failure (see the clean-architecture skill / `Result`
pattern); the wrapper is optional but recommended for consistency.

```csharp
// Marker contracts — intent only.
public interface ICommand;                 // command, no response
public interface ICommand<TResponse>;      // command, returns a value
public interface IQuery<TResponse>;        // query, always returns a value

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}
```

These deliberately mirror MediatR's `IRequest`/`IRequestHandler` shape, so migrating off MediatR later
is mechanical.

## A command handler

```csharp
public sealed record CompleteTodoCommand(Guid TodoItemId) : ICommand;

internal sealed class CompleteTodoCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<CompleteTodoCommand>
{
    public async Task<Result> Handle(CompleteTodoCommand command, CancellationToken cancellationToken)
    {
        TodoItem? todoItem = await context.TodoItems.SingleOrDefaultAsync(
            t => t.Id == command.TodoItemId && t.UserId == userContext.UserId,
            cancellationToken);

        if (todoItem is null)
        {
            return Result.Failure(TodoItemErrors.NotFound(command.TodoItemId));
        }

        todoItem.MarkCompleted(dateTimeProvider.UtcNow);   // domain behaviour, not setters
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

Handlers are `internal sealed` (registered by DI scanning, consumed via their interface). All business
logic — lookup, validation, state change, persistence — lives in the handler, not the endpoint.

## Cross-cutting concerns — decorators, not pipeline behaviours

Wrap handlers with the **decorator** pattern for logging, validation, transactions, etc. Each decorator
targets one generic handler contract and delegates to the inner handler, so you need one decorator
class per contract shape (`ICommandHandler<>`, `ICommandHandler<,>`, `IQueryHandler<,>`).

```csharp
internal sealed class ValidationCommandHandler<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> innerHandler,
    IEnumerable<IValidator<TCommand>> validators)
    : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
    {
        ValidationFailure[] failures = await Validate(command, validators);
        if (failures.Length == 0)
        {
            return await innerHandler.Handle(command, cancellationToken);
        }

        return Result.Failure<TResponse>(CreateValidationError(failures));
    }
    // ... Validate + CreateValidationError helpers
}
```

Validation uses **FluentValidation** (matches the API skill's layered-validation rule). Keep each
decorator to a single concern; layer them transparently.

## DI wiring (Scrutor)

Register all handlers by assembly scan, then apply decorators. Reflection runs **once at startup** —
predictable, no per-request magic.

```csharp
services.Scan(scan => scan.FromAssembliesOf(typeof(DependencyInjection))
    .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
        .AsImplementedInterfaces().WithScopedLifetime()
    .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
        .AsImplementedInterfaces().WithScopedLifetime()
    .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
        .AsImplementedInterfaces().WithScopedLifetime());

// Decorate: last applied = outermost at runtime. Logging outermost so it captures validation exits.
services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationDecorator.CommandHandler<,>));
services.Decorate(typeof(ICommandHandler<,>), typeof(LoggingDecorator.CommandHandler<,>));
services.Decorate(typeof(IQueryHandler<,>), typeof(LoggingDecorator.QueryHandler<,>));
```

## Calling a handler (no mediator)

Inject the specific handler interface directly into the endpoint/controller and call `Handle`. No
`ISender`, no runtime lookup — the container resolves it explicitly.

```csharp
// Minimal API / FastEndpoints / controller — endpoint stays thin.
app.MapPut("todos/{id:guid}/complete", async (
    Guid id,
    ICommandHandler<CompleteTodoCommand> handler,
    CancellationToken cancellationToken) =>
{
    Result result = await handler.Handle(new CompleteTodoCommand(id), cancellationToken);
    return result.Match(Results.NoContent, CustomResults.Problem);
});
```

## Read/write model separation — start simple, grow only if needed

1. **Same store, separate models (default).** One database; commands go through the domain model,
   queries read via lean projections/DTOs (Dapper or EF `.Select`). This is "CQRS-lite" and covers
   most apps. No sync problem — one source of truth.
2. **Separate read store / replicas.** Split only when reads must scale or be shaped very differently
   (denormalised/materialised views, a read replica, a document DB for reads). Now you must keep them
   in sync and accept **eventual consistency**.
3. **Sync strategies** (pick per requirement): read replicas / built-in replication (simple, strong-ish
   consistency) · CDC (near-real-time) · a cache (Redis) with clear invalidation · domain events +
   message broker (scalable, eventually consistent) · materialised views · Event Sourcing (audit trail
   + temporal queries, highest complexity). When crossing a store + broker boundary, use the
   **Transactional Outbox** to persist state + event atomically and make read-side consumers
   **idempotent**. Only reach for these when a concrete need exists.

## Do / Don't

- **Do** put commands/queries + handlers under `Application/Features/<Feature>/…`; one file per type.
- **Do** keep queries free of domain logic and return DTOs; keep commands business-task-shaped.
- **Do** own the `ICommand`/`IQuery` abstractions; use decorators + Scrutor for cross-cutting concerns.
- **Don't** add MediatR (or any mediator library) to a new project by default.
- **Don't** introduce separate read/write databases, messaging, or Event Sourcing speculatively.
- **Don't** apply CQRS to a simple CRUD domain — a plain data-access layer is the right call there.
- **Don't** let endpoints/controllers hold logic — they build the command/query and call the handler.
