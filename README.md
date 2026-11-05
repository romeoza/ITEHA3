# Vulindlela

Vulindlela ("make way" / "open the road" in isiZulu) is a Phase 1 prototype for a civic counter-services platform. It lets a citizen see real branch availability, book a specific counter slot, submit supporting documents in advance, and be told before travelling whether anything is missing. Officials get an overview of all bookings, and every read of a citizen's record is logged and visible to that citizen.

The prototype runs beside the existing counter system rather than replacing it. Booking and document clearance are separate services with separate databases, and a failure in clearance does not stop a citizen from booking.

## Architecture

```
                 browser
                    |
                    v
        +---------------------------+
        |  Portal   (MVC, :5100)    |   Razor views, cookie sign-in
        +------+------------+-------+
               |            |
     cookie    |            |  HTTP (typed client, interface-wrapped)
     forwarded |            |
               v            v
 +----------------------+   +-------------------------+
 | Booking API (:5101)  |   | Clearance API (:5102)   |
 | bookings, slots,     |   | document submissions,   |
 | branches, access log |   | status, Channel queue   |
 | booking.db (SQLite)  |   | clearance.db (SQLite)   |
 +----------------------+   +-------------------------+
            |                           ^
            +--- notify on booking -----+   (failure is caught and logged)
```

- **Portal** (`src/Vulindlela.Portal`): ASP.NET Core MVC. Composes "My Appointments" from both APIs at read time and degrades per section when a service is unavailable.
- **Booking API** (`src/Vulindlela.Booking.Api`): owns slots, branches, citizens, appointments and the access log. Enforces the booking rules.
- **Clearance API** (`src/Vulindlela.Clearance.Api`): accepts document submissions, checks them against the rules for the service type, and processes them on a background worker through a `System.Threading.Channels` queue. The verification is simulated with a 3-second delay.
- **Shared** (`src/Vulindlela.Shared`): DTOs and the service-type fee table, shared by all three projects.

Design decisions are recorded in [docs/adr](docs/adr):
- [0001 service boundaries](docs/adr/0001-service-boundaries.md)
- [0002 preventing double booking](docs/adr/0002-preventing-double-booking.md)
- [0003 queue and background worker](docs/adr/0003-queue-and-background-worker.md)

## Requirements

- Windows with the .NET 8 or later SDK (this was built and tested with .NET 10 SDK, 10.0.402).
- No database server, message broker or cloud account. SQLite files are created on first run.

## How to run

**Option 1: one command (recommended).** From the repo root in PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File .\start-dev.ps1
```

The script creates `C:\tmp`, builds the solution, starts all three services minimised, waits for their ports, and opens `http://localhost:5100/Account/SignIn`.

**Option 2: manually.**

```powershell
dotnet build Vulindlela.slnx
dotnet run --project src/Vulindlela.Booking.Api     # http://localhost:5101
dotnet run --project src/Vulindlela.Clearance.Api   # http://localhost:5102
dotnet run --project src/Vulindlela.Portal          # http://localhost:5100
```

Start the Booking API first. It creates `booking.db` and seeds it on first run. Delete the `.db` file to reset the data.

**Ports**

| Service   | URL                     |
|-----------|-------------------------|
| Portal    | http://localhost:5100   |
| Booking   | http://localhost:5101   |
| Clearance | http://localhost:5102   |

**Stop everything**

```powershell
Get-Process Vulindlela* | Stop-Process -Force
```

Running the tests:

```powershell
dotnet test Vulindlela.slnx
```

## Seeded accounts

All accounts use the password **`Passw0rd!`**.

| Username   | Role     | Full name        | Use it to                                              |
|------------|----------|------------------|--------------------------------------------------------|
| `thabo`    | Citizen  | Thabo Mokoena    | Book, submit documents, view own access log            |
| `naledi`   | Citizen  | Naledi Dlamini   | As above, a second citizen for "cannot see others"     |
| `sipho`    | Citizen  | Sipho Nkosi      | As above                                               |
| `official` | Official | Official Mahlangu| View the official overview of all bookings             |

Seeded branches: Johannesburg Central (`JHB-CENTRAL-01`), Soweto (`JHB-SOWETO-02`) and Pretoria (`PTA-PRETORIA-03`). Each has half-hour slots from 08:00 to 16:00 UTC, Monday to Friday, for the next 14 days.

## Things to try

1. **Book a slot.** Sign in as `thabo`, go to **Book**, pick a branch, a service and a slot. You should see "Booking confirmed."
2. **Test the one-per-service rule.** Try to book the same service again while the first appointment is still upcoming. The booking is refused with a message explaining why.
3. **Submit documents.** On **My Appointments**, use **Submit documents** for a passport or birth certificate booking. Leave out a required document and the status shows what is missing, before you travel.
4. **See degradation.** Stop the Clearance API (close its window or run the stop command), then reload **My Appointments**. Existing bookings still show, with "Document clearance unavailable — booking is confirmed" in place of the clearance status. Booking still works.
5. **Read your access log.** Open **Access Log** as `thabo`. Every appointment lookup and official overview that touched your record is listed with who read it, their role, the time, and the reason.
6. **Official overview.** Sign in as `official`. The **View details** button in the top bar opens `/Official`, with bookings per branch and a full list of bookings. Each record shown is logged against the citizen it belongs to. Then sign in as a citizen and visit `/Official` directly: you are redirected to Access Denied.
7. **Concurrent booking.** The test suite fires many simultaneous requests at one slot and asserts exactly one appointment survives: `dotnet test Vulindlela.slnx`.

## Known limitations

These are deliberate gaps for a Phase 1 prototype. Several are listed so a reviewer knows what has not been done.

- **One-per-service check is not atomic.** The rule "one upcoming appointment per service type" is a check followed by an insert. Two requests from the same citizen sent at the same moment could both pass it. Slot double-booking does not have this problem, because the unique index on the slot is the guard. A database-level guard for the citizen rule is not implemented.
- **Document verification is simulated.** The Clearance worker waits three seconds and then marks the submission `Complete` or `CompleteWithMissing`. There is no OCR, no population-register lookup and no virus scan. Only two service types have document rules (passport and birth certificate).
- **Document names are free text.** Submitted documents are matched by name against the required list, so a typo counts as missing.
- **Data-protection keys are written to `C:\tmp`.** Both the Booking and Portal projects persist ASP.NET Core data-protection keys to `/tmp/vulindlela-keys`, which resolves to `C:\tmp` on Windows. This is hard-coded and only suits a local Windows prototype. Move it into the project before running on another machine.
- **Clearance is not authenticated.** The Clearance API trusts the citizen and appointment IDs it receives. Only the portal and Booking API are protected by sign-in.
- **Sign-in is not production-grade.** Cookies are issued by both the portal and the Booking API. There is no lockout, no password reset, no MFA, and no HTTPS redirect in development.
- **No CSRF tokens on forms.** Portal POST forms (booking, submit documents, sign-out) do not use anti-forgery tokens.
- **Official overview is unpaginated.** It loads every appointment in one request, which is fine for seed data and will not scale.
- **Times are stored in UTC.** The portal displays them in the server's local time zone. The seeded slots are on UTC hours, so the displayed times depend on the machine's time zone.
- **Fees are formatted by culture.** On a South African locale the fee shows with a comma decimal (for example `R865,0`).
- **No persistence beyond SQLite files.** Each service has one `.db` file in its project folder. There are no migrations; schema changes need the file deleted.
- **Access log reads are not tamper-evident.** Entries are plain rows in SQLite. There is no hash chain or signing, so an administrator with file access could alter them.

## Repository layout

```
docs/adr/                 Architecture decision records
legacy/                   (in project_starter) the BranchDesk 2011 class analysed in Deliverable 1
src/Vulindlela.Booking.Api/
src/Vulindlela.Clearance.Api/
src/Vulindlela.Portal/
src/Vulindlela.Shared/    DTOs and fee table shared by the services
tests/Vulindlela.Booking.Tests/   xUnit tests against a real SQLite in-memory database
start-dev.ps1             build, start all services and open the portal
Vulindlela.slnx           solution file
```

## Licence and status

Academic prototype for a formative assessment. Not for production use.

## Assignment deliverables

This section is taken from the ITEHA3 formative assignment write-up. It describes what the assignment asked for and how the prototype addresses each part. Where the write-up's code samples differ from the code in `src/`, the code is the source of truth.

### Deliverable 1: Analysis and architecture

#### 1.1 Legacy class analysis

The legacy class is `project_starter/legacy/LegacyAppointmentManager.cs`, a BranchDesk 2011 class. The six problems chosen are:

| No | Problem | Evidence in the code | Risk to the Agency | SOLID principle violated |
|----|---------|----------------------|--------------------|--------------------------|
| 1 | SQL injection on every query path | `"...WHERE BranchCode = '" + branchCode + "' AND AppointmentDate = '" + appointmentDate + "'"` and the INSERT, and `"WHERE FullName LIKE '%" + searchTerm + "%'"` in `FindCitizen` | A citizen (or a clerk typing a surname with an apostrophe) can corrupt or read the whole Appointments table. `searchTerm` is the worst: it is free text typed at a counter. The connection uses `sa`, so injection reaches the whole server, not just this database. | SRP (validation, SQL, SMS, export and printing are all in one method). Also Open/Closed: every new service type means editing this method. |
| 2 | Check-then-act race condition on the slot | `SELECT COUNT(*) ... WHERE AppointmentDate = ...` followed by an INSERT, with no transaction, no lock and no unique constraint. `taken >= 40` is read, then the row is written. | This is the thundering-herd bug in miniature. Two clerks (or two branches sharing the row) both see 39, both insert, and 41 people arrive for 40 slots. The code meant to solve the people-turned-away problem reproduces it. | SRP (the booking rule lives inline in a data-access method). |
| 3 | One method does seven unrelated jobs | Validation, fee calculation, capacity check, insert, file-system document check, SMTP send, CSV export, console print, all inside `BookAppointment`. | Any change to the SMS gateway, the export format or the fee table forces a re-test of the whole booking path. The 2019 maintainer's comment ("I could not find a better place to put it") is direct evidence. A bug in printing can stop a booking being recorded. | SRP, at class and method level. The class has at least six reasons to change. |
| 4 | Secrets and connection strings as literals in source | `"Server=BRANCHSRV01;...User Id=sa;Password=Branch2011!;"`, `smsGatewayKey = "SMSGW-7741-PROD-9930"`, hardcoded `branchCode` and `headOfficeExportPath`. | Anyone with the source (or a decompiler, or the git history of the 218 per-branch builds) has the `sa` password to every branch server and the live SMS gateway key. Rotating a key means rebuilding and redeploying 218 binaries. | DIP (the class constructs its own `SqlConnection` and `SmtpClient`, with no interface and no injection). |
| 5 | Silent failure on the two outbound side-effects | `catch (Exception) { }` around the SMTP send and the CSV export, with comments "The citizen will find out on the day" and "Ignore. The overnight job will pick it up tomorrow. Usually." | The scenario's stated failure, a locked export file losing a day's bookings silently, is written into the code as intended behaviour. The citizen is never told the reminder failed, and there is no log, so nobody can measure how often it happens. | SRP, specifically: the error policy is buried inside the business method and cannot change independently. |
| 6 | No audit trail, and unbounded citizen search | `FindCitizen` takes a free-text `searchTerm`, returns every matching row, and its comment says "There is no record anywhere of who ran this or when." No `Authorize`, no identity parameter, no logging. | This is the failure Tallinn was cited to fix. Any clerk can look up any citizen, repeatedly, with no record. The citizen cannot see who looked, so unauthorised access is undetectable. | SRP (the query method holds no policy), and it breaks the Agency's own access-logging constraint. |

**Why these six.** Each maps onto a requirement the Agency has stated: the race maps onto Deliverable 2.3, the secrets onto 4.4, the missing audit onto 4.3, the silent failures onto 3.4 (surviving a component's failure without an unhandled exception), and the monolithic method onto the layered-architecture requirement. The remaining problems (the 13-digit ID check that does not validate the check digit, the decimal fee stored as a formatted string in CSV, the SMTP key leaked in the message body, and `MailMessage` used as an SMS transport) are real but follow from the six above rather than being independent failures.

#### 1.2 Service boundaries

**Target architecture**

- **Booking owns** branches, slots and appointments. It is the only writer of `booking.db`.
- **Clearance owns** document requirements, submissions and their outcomes. It is the only writer of `clearance.db`.
- **Portal owns** no persistent data beyond the cookie. It composes at read time.
- **Communication:** portal calls both services (read); Booking calls Clearance (write, on appointment creation). There is no Clearance-to-Booking call. A Clearance failure therefore cannot stop a booking, because Booking never waits on Clearance.

**Why the boundary is there.** The boundary follows the two things Phase 1 takes over (booking and document clearance), not the existing BranchDesk modules and not the 218 branches. The two are separate because they fail separately and because their data has different lifetimes. A booking is a commitment to a slot at a time: it must be fast, transactional (exactly one appointment per slot) and must succeed even when nothing else does. A clearance is slow, retryable and idempotent: a document can be resubmitted, a scan re-run, a result can arrive an hour later. In one service, the slow verification path would share a request pipeline and a database with the booking. The first time a virus scan took ten seconds, the booking counter would time out, which is the "one component taking down everything" failure of the March incident. Separate services with separate databases make the slow path physically unable to block the fast one.

The boundary is not drawn between "documents" and "document verification", or between "citizens" and "appointments". Those would create chatter: every booking would need a synchronous call to a citizen service to validate the ID, and every verification would need a synchronous call back to bookings to confirm the slot. A boundary in the wrong place costs a network round-trip per operation and creates a distributed transaction where a single one would do. Here the only cross-service call is one fire-and-return notification from Booking to Clearance, the minimum chatter consistent with the Agency's rule that Booking must never reference Clearance's database.

#### 1.3 Architecture decision records

- [0001 service boundaries](docs/adr/0001-service-boundaries.md)
- [0002 preventing double booking](docs/adr/0002-preventing-double-booking.md)
- [0003 queue and background worker](docs/adr/0003-queue-and-background-worker.md)

#### 1.4 Enterprise architecture framework

**Framework chosen: TOGAF** (the Architecture Development Method, ADM, and its Requirements Management phase).

Zachman classifies artefacts (What, How, Where, Who, When, Why) and stakeholder rows. It can say a "Slot" artefact exists at the logical and physical levels, but it does not say what to do next. Phase 1 is a delivery problem: two services, explicit constraints, six weeks, and a marker who must run it from a clean clone. TOGAF's ADM gives a sequence that maps onto the deliverables.

| TOGAF ADM phase | Phase 1 work |
|-----------------|--------------|
| Preliminary | Setup: .NET 8, repository, `.gitignore`, the supplied ADR template |
| Architecture Vision | The problem statement: 218 branches, a 03:00 queue, one in three turned away. The Kigali and Tallinn ideas. The three non-negotiable constraints. |
| Business Architecture | The two capabilities Phase 1 takes over: slot booking and document clearance. The citizen-before-travel outcome, and the access log as a deterrent control. |
| Information Systems: Data | Appointment, Slot and Branch in `booking.db`; DocumentSubmission and clearance result in `clearance.db`. Physical separation is recorded in ADR 0001. |
| Information Systems: Application | Two Web APIs plus the MVC portal; `IClearanceClient` and `ISubmissionQueue` as the integration seams. |
| Technology Architecture | The prescribed stack: ASP.NET Core, EF Core 8, SQLite, Channels, `BackgroundService`, typed `HttpClient`, cookie auth, PBKDF2, policy-based authorisation, xUnit. |
| Opportunities and Solutions / Migration | The three ADRs (boundaries, double-booking guarantee, async queue). The "runs beside BranchDesk" migration statement. |
| Requirements Management | Traceability from each Agency constraint to a deliverable and a test. |

**Justification against a specific problem.** Take the constraint "a failure in document clearance must not stop a citizen booking". TOGAF makes it traceable end to end: it is a business requirement (the March incident), it drives an application decision (two services, a fire-and-return `IClearanceClient`, ADR 0001), a technology decision (no shared transaction, a caught-and-logged call), and an acceptance test (the Clearance-stopped evidence in Deliverable 3.4). Zachman describes the requirement in each row but does not force the chain from requirement to test.

**What TOGAF will not solve on its own.** TOGAF does not tell you how to make the double-booking guarantee hold. It organises architecture work; it does not reason about concurrency. The unique-index-versus-lock decision in ADR 0002, and the test that proves it against a real SQLite database, are engineering judgement that TOGAF frames but does not supply.

#### 1.5 Project structure

The solution separates services (`src/`) from tests (`tests/`), with documentation in `docs/` and the supplied legacy class in `project_starter/`.

### Deliverable 2: The booking service

#### 2.1 Three layers

- **Presentation (Controllers).** Receives the request, validates the model, calls the business layer and shapes the response. Holds no rules and no SQL. See `src/Vulindlela.Booking.Api/Controllers/AppointmentsController.cs`.
- **Business (`Business/`).** Holds every rule about what a valid booking is: the slot exists, the service type is known, the fee is computed, the appointment is created, and the clearance notification is attempted. It never touches SQL. See `BookingService.cs`.
- **Data access (`Data/`).** `Repositories.cs` and `BookingDbContext.cs`, EF Core against `booking.db`, each behind an interface. The unique index on `SlotId` is the enforcement point recorded in ADR 0002.

Every dependency is constructor-injected and registered in `Program.cs`. No class creates its own dependencies with `new`, except the composition root and the worker's `CreateScope()`.

#### 2.2 Seed data

On first run the Booking API calls `EnsureCreated()` and then `SeedData.EnsureSeededAsync`. This creates three branches (`JHB-CENTRAL-01`, `JHB-SOWETO-02`, `PTA-PRETORIA-03`), two weeks of half-hour slots per branch on weekdays from 08:00 to 15:30 UTC, and four demonstration accounts (`thabo`, `naledi`, `sipho` and `official`) with PBKDF2-hashed, per-user-salted passwords. Deleting `booking.db` re-seeds on the next run.

#### 2.3 Double-booking guarantee

The guarantee is the unique index on `SlotId`, enforced by SQLite and treated as the authoritative answer by the business layer (ADR 0002). The booking path does one INSERT; a unique-constraint violation is reported as a conflict, not retried. There is no pre-check. The rejected alternatives (check-then-act with retry, serializable transactions, an in-process lock, a row version) and the costs of the chosen approach are in [docs/adr/0002-preventing-double-booking.md](docs/adr/0002-preventing-double-booking.md).

#### 2.4 Tests

`tests/Vulindlela.Booking.Tests/BookingRulesTests.cs` covers an unknown slot, a second booking of the same slot, an ineligible service type, the fee table, and the one-upcoming-appointment-per-service rule.

`ConcurrencyTests.cs` is the load-bearing test. It uses a shared in-memory SQLite database and fires N concurrent requests at one slot (N = 2, 10, 50), asserting that exactly one appointment survives. Each request uses its own citizen, so only the slot guard is under test. It runs against real SQLite, so the unique index is actually exercised; the test deliberately does not use a fake repository, because a fake would pass while the real system double-booked.

### Deliverable 3: The document clearance service

#### 3.1 Clearance service

A second layered Web API on port 5102 with its own `clearance.db`:

- `POST /api/clearance/submissions` accepts an appointment, citizen, service type and document names, records the submission as `Pending`, enqueues it and returns straight away. It never waits.
- `GET /api/clearance/status/{appointmentId}` returns the current status and any missing documents.
- The per-service-type requirements (passport: certified ID copy and passport photograph; birth certificate: parent ID copy) live in `ClearanceService.cs`.

Booking holds no connection string for `clearance.db` and never queries it. It only calls Clearance through `IClearanceClient`.

#### 3.2 Asynchronous slow work

`ISubmissionQueue` wraps a `Channel`. The controller enqueues and returns. `ClearanceWorker : BackgroundService` drains the channel, creates a scope per item to resolve its scoped repository, simulates the slow work with a three-second delay, evaluates the documents and writes the outcome. The scope-per-item avoids injecting a scoped context into a singleton. The interface is the seam for a future broker. The reasoning is in [ADR 0003](docs/adr/0003-queue-and-background-worker.md).

#### 3.3 Service communication

`IClearanceClient` is implemented by a typed `HttpClient` registered in `Program.cs`, with its base address read from configuration. `BookingService` depends on the interface, not on HTTP. The notification is wrapped in a `try`/`catch` that logs a warning and lets the booking succeed, so the caller never sees a network exception.

#### 3.4 Surviving a Clearance failure

Stopping Clearance changes nothing a citizen can see at booking time: the appointment is still written, the confirmation page renders, and the portal still loads. On **My Appointments**, a failed clearance call for a row shows the badge "Document clearance unavailable — booking is confirmed" instead of an error page.

Evidence required by the brief (to be captured by the student and included in the report, not in this README):

- Screenshot 1: the booking confirmation while the Clearance process is stopped.
- Screenshot 2: My Appointments showing the degradation message and no unhandled-exception page.

#### 3.5 Citizen view

`MyAppointmentsController` calls Booking for the appointment list and Clearance for each status, composing them at read time. Each call is guarded separately: if Booking responds, the list renders; if Clearance is down, the status column says so plainly. The page does not throw.

### Deliverable 4: Security, audit and delivery

#### 4.1 Authentication

Cookie authentication (`AddAuthentication().AddCookie(...)`) in both the portal and the Booking API. Passwords are stored as PBKDF2 hashes with a per-user salt (`Business/PasswordHasher.cs`), never in clear text and never reversibly encrypted. `[Authorize]` protects the booking, appointments, access-log and official pages.

#### 4.2 Policy-based authorisation

A role check alone cannot express "a citizen may view only their own appointments and their own access log", because every citizen holds the same role. The rule is implemented as a policy (`CitizenOwnsRecord`) with a requirement (`OwnsRecordRequirement`) and a handler (`OwnsRecordHandler` in `Business/Authorization.cs`). The official overview is restricted with `[Authorize(Roles = "Official")]`.

#### 4.3 Access log

Every read of a citizen's record writes an `AccessLog` row recording who read it, their role, when, why and which resource. Citizens can read their own log at **Access Log**. Officials' reads of any record are logged against the citizen concerned. An audit trail only an auditor can read is a control; this one is visible to the subject, which makes it a deterrent.

#### 4.4 Keeping secrets out of source

No connection string, API key or password appears as a literal in any `.cs` file. The Booking connection string and the Clearance base URL are read through `IConfiguration` from `appsettings.json`. The `.gitignore` excludes `*.db`, `bin/`, `obj/` and `.vs/`. The legacy class is the counter-example: it does the opposite.

#### 4.5 README

This README contains the problem statement, the architecture diagram, run instructions with ports, seeded accounts, things to try, and known limitations, as the brief requires.

#### 4.6 Git history and pull requests

Work is committed on feature branches and merged through pull requests. The branch graph and pull request screenshots required by the brief are to be captured by the student and included in the report.
