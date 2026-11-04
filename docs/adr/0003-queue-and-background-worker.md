# ADR 0003: Use Channel<T> behind ISubmissionQueue and a BackgroundService for slow verification

- **Status:** Accepted
- **Date:** Phase 1, week 3

## Context

Verifying a document is slow: in production it means OCR, a lookup against the
population register and a virus scan. The submission endpoint must record the
submission and return immediately; a background worker drains the queue and
updates state as it goes. The brief prescribes `System.Threading.Channels` and
`BackgroundService`, both in the framework, and requires the queue to be
behind an interface so that in production only one class would change.

A BackgroundService is registered as a singleton, which means it cannot
inject a scoped `DbContext` directly. This is the bug almost everyone hits
here.

## Decision

The queue is `Channel<DocumentSubmission>` wrapped in `ISubmissionQueue`
(`EnqueueAsync` / `ReadAllAsync`). The submission controller enqueues and
returns `202 Accepted` with the submission id. A `ClearanceWorker :
BackgroundService` reads from the channel and, for each submission, creates a
scope, resolves a scoped `IClearanceRepository`, simulates the slow work with
a `Task.Delay` of at least three seconds, and writes the outcome.

    // ClearanceWorker
    await foreach (var submission in _queue.ReadAllAsync(stoppingToken))
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IClearanceRepository>();
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        await repo.RecordOutcomeAsync(submission.Id, Evaluate(submission), stoppingToken);
    }

## Alternatives considered

**Verify synchronously inside the submission endpoint.** Rejected. The brief
requires the endpoint not to wait, and the three-second delay would make the
portal feel broken and would hold an HTTP connection open. It also couples the
slow path to the request lifetime, which is the failure the whole service
boundary exists to avoid.

**`BlockingCollection<T>` or a `ConcurrentQueue<T>`.** Rejected. Neither gives
async back-pressure or `IAsyncEnumerable` consumption, so the worker would
either block a thread or busy-poll. `Channel<T>` is in the framework and is
what the brief prescribes.

**A hosted service that injects `IServiceProvider` and calls
`GetRequiredService` per item, but without a scope.** Rejected. It resolves
the scoped repository from the root provider, which either throws or returns
a long-lived context shared across concurrent items — the classic
"BackgroundService singleton holds a scoped DbContext" bug. The explicit
`CreateScope()` per item is the fix, and it is the reason this ADR exists.

**A real broker (RabbitMQ / Azure Service Bus).** Rejected for Phase 1. It is
the production shape, but it needs an install the marker does not have. The
`ISubmissionQueue` interface is the seam: replacing the channel with a broker
client is one class.

## Consequences

**What we gain.** The endpoint returns in milliseconds; the three-second work
is visible in a demonstration as state moving from Pending to Complete. The
queue is an interface, so the broker swap is one class. The worker owns its
own scope per item, so the scoped-context bug is designed out.

**What we accept.** The channel is in-process and unbounded by default, so a
crash loses queued submissions; for Phase 1 that is acceptable because a
submission can be re-sent, and the submission row is written before enqueue so
the loss is visible rather than silent. We must also handle shutdown
gracefully (`stoppingToken`) so a drain does not leave submissions half-done.
A bounded channel with `FullMode = Wait` would add back-pressure but would make
the endpoint block when full, which reintroduces the coupling we removed; this
trade is recorded and revisited if volume grows.