# ADR 0001: Separate Booking and Clearance into two services with separate databases

- **Status:** Accepted
- **Date:** Phase 1, week 1

## Context

Phase 1 must take over two things from BranchDesk 2011: slot booking and
document clearance. The Agency has set a non-negotiable constraint that these
are separate services with separate databases, neither reading the other's
tables, and that a failure in clearance must not stop a citizen booking. The
March incident, in which one failing component took down everything, is the
stated reason for the constraint.

The two workloads also have different shapes. Booking is a short, transactional,
latency-sensitive write that must never wait. Clearance is a long, retryable,
idempotent process — OCR, a population-register lookup and a virus scan — that
the brief itself says must be done asynchronously.

## Decision

We run two independently deployable ASP.NET Core Web APIs, each with its own
SQLite file. Booking owns `booking.db` (Branch, Slot, Appointment, AccessLog,
Citizen) and exposes slot and appointment endpoints. Clearance owns
`clearance.db` (DocumentRequirement, DocumentSubmission, ClearanceResult) and
exposes a submission endpoint plus a status endpoint. The Portal composes the
two at read time.

The only cross-service call is Booking → Clearance, on appointment creation,
through `IClearanceClient` (a typed `HttpClient`). It is fire-and-return and
its failure is caught and logged, not propagated. Clearance never calls
Booking. Neither service holds the other's connection string.

## Alternatives considered

**One service with two DbContexts over one SQLite file.** Rejected. It would
satisfy "two layers of code" but not "neither may read the other's tables":
with one file, the boundary is a convention in code, not a physical fact, and
the first convenient `JOIN` breaks it silently. It also puts the three-second
clearance work in the same request pipeline as the booking write, which is the
March incident's failure mode. The Agency's constraint is explicit and this
option cannot honour it.

**Two services sharing one SQLite file, opened read-only by the reader.**
Rejected. SQLite's single-writer lock means the clearance worker's writes
would block the booking writer under load, which is the opposite of the
independence the constraint exists to buy. It also gives Clearance a
connection string to Booking's database, which the brief forbids.

**A shared message broker (RabbitMQ) between the services.** Rejected for
Phase 1. It is closest to the production shape, but it requires Docker or a
server install, and the brief requires a clean clone to run with the SDK
alone. `System.Threading.Channels` gives the in-process queue the brief asks
for, behind `ISubmissionQueue`; swapping in a broker later changes one class.

**A shared `Vulindlela.Shared` project for DTOs.** Accepted, with a caveat
recorded in ADR 0003: it holds only the DTOs that cross the boundary, never
entities, and never a DbContext.

## Consequences

**What we gain.** Clearance can be stopped, restarted or redeployed without a
booking failing; this is demonstrated deliberately in Deliverable 3.4. The two
databases are physically separate files, so "no shared database" is something
a marker can see rather than something they must trust. The slow path cannot
block the fast path. Each service can be scaled or replaced independently.

**What we accept.** The Portal must compose two sources at read time, and must
handle one being unavailable without failing the page — extra code that a
single database would not need. There is no cross-service transaction, so an
appointment can exist whose clearance submission was never accepted; this is
tolerable because a missing submission is visible and retryable, whereas a
failed booking is not. We pay a network hop on appointment creation, and we
must define DTOs at the boundary rather than share entities.