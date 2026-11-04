# ADR 0002: Prevent double booking with a unique index on SlotId, not a check-then-act

- **Status:** Accepted
- **Date:** Phase 1, week 2

## Context

A slot has capacity exactly one. When booking opens, many requests hit the same
slot in the same second — the thundering herd. The obvious implementation is
check-then-act:

    if (!await _slotRepo.SlotIsTaken(slotId))
        await _appointmentRepo.Add(appointment);

Two requests can both pass the check before either acts. The window is small;
under load it is hit routinely. A test against a fake repository that ignores
database constraints will pass while the real system double-books, which is
worse than no test because it reports a false success.

## Decision

The guarantee is enforced by the database, not by application code. The
`Appointment` table has a unique index on `SlotId`, and the booking path does
a single `INSERT` inside a transaction and treats a unique-constraint
violation as "slot already taken":

    // DAL
    var appointment = new Appointment { SlotId = slotId, CitizenId = citizenId, ... };
    _context.Appointments.Add(appointment);
    try
    {
        await _context.SaveChangesAsync();   // unique index on SlotId is the guard
        return BookingResult.Booked(appointment.Id);
    }
    catch (DbUpdateException ex) when (IsUniqueViolation(ex))
    {
        return BookingResult.SlotTaken();
    }

The business layer never asks "is it free?" before writing. The unique index
on `SlotId` is created by `EnsureCreated()` from the model configuration, and
the test that proves it runs against a real shared in-memory SQLite database
using the connection-string fixture from SETUP.md, not a fake.

## Alternatives considered

**Check-then-act with a retry loop.** Rejected. Adding a retry does not close
the window; it makes the window narrower and the bug rarer and therefore
harder to find. It is the exact implementation the brief calls out as wrong.
It also cannot be made correct without a lock, at which point it is a worse
version of the option below.

**Serializable transaction or `BEGIN IMMEDIATE` around the read and write.**
Rejected. It is correct, but SQLite serialises all writers anyway, so this
buys correctness at the cost of a coarser lock that blocks unrelated bookings
on other slots. It also depends on the provider's isolation semantics rather
than on a schema invariant, so a later move to SQL Server or PostgreSQL would
need the logic re-reviewed. The unique index survives a provider change
unchanged.

**Application-level lock (`SemaphoreSlim` or `lock`).** Rejected. It only
works inside one process, and the target architecture runs several instances
of Booking — the brief's own March incident is about what happens when one
component assumes it is alone. A lock in one process does nothing for the
second instance.

**Optimistic concurrency with a `rowversion` column.** Rejected. It is the
right tool when an existing row is being *updated* by two writers, which is
not this case: the contention is on *inserting* a row that must be unique.
A unique index states the invariant directly; `rowversion` would require a
pre-read, which reintroduces the check-then-act shape.

## Consequences

**What we gain.** The guarantee is a database invariant, so it holds across
processes, across instances, and under any concurrency the test can generate.
The failing case is a single, named exception, so the business layer reports
"slot taken" honestly instead of optimistically. The test can assert the real
behaviour, because it runs against real SQLite.

**What we accept.** The unique index is on `SlotId`, so the model must keep
appointments and slots in the same database — a later move to "one appointment
row per slot per date" would need the index widened, not just moved. A
constraint violation is reported as a generic `DbUpdateException` whose inner
exception we must inspect to distinguish "slot taken" from an unrelated write
failure; the `when (IsUniqueViolation(ex))` filter is load-bearing and is
tested. SQLite allows only one writer at a time, so throughput on a single
file is bounded; that is accepted for Phase 1 and is the same trade recorded
in the worked example ADR.