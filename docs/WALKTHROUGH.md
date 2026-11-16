# Talking through Meridian

This is the plain-language tour of the repository. It is written so you can walk a hiring manager through the design without reading the code alongside them. The sample crew, sites, and assets are invented.

## What the product is

Meridian is a work-order and inspection tool for energy field crews. A technician at a substation, a generation station, or a compressor station opens a job against a tagged asset, moves it through a small set of statuses, and leaves notes for the next shift. A supervisor sees how many jobs are open, overdue, or waiting on parts, and how the active work splits by priority.

That matches the kind of operational software energy companies actually run, and it is small enough to explain end to end.

## Why the API is split into four projects

`Meridian.Domain` has no packages. It knows what a work order is allowed to do, and nothing about HTTP or SQL. That is the part you want to unit test without booting a server.

`Meridian.Application` is the use cases: "open a work order", "log in", "record an inspection". Each use case is a MediatR request, a FluentValidation validator, and a handler. Handlers depend on interfaces (`IWorkOrderRepository`, `IClock`, `ICurrentUser`), not on EF types.

`Meridian.Infrastructure` implements those interfaces with EF Core, ASP.NET's password hasher, and SHA-256 hashed refresh tokens.

`Meridian.Api` is the HTTP edge: controllers, JWT validation, Serilog, Swagger, and the exception handler that turns domain failures into problem details.

The dependency arrows only point inward. The domain does not reference the API. If someone asks "could you swap the database?", the answer is that you would replace Infrastructure and keep the handlers.

## One request, from the phone to the database

Take "complete a work order":

1. The Flutter detail screen posts `{ "status": "Completed", "laborHours": 1.5 }` to `/api/work-orders/{id}/transitions` with the bearer token.
2. JWT middleware authenticates the caller. The controller only builds a `TransitionWorkOrderCommand` and sends it.
3. `ValidationBehavior` runs FluentValidation before the handler. A labor value of `-1` never reaches the domain.
4. The handler loads the tracked work order, checks that the caller created it, is assigned to it, or is a supervisor, then calls `WorkOrder.TransitionTo`.
5. The aggregate refuses illegal edges and refuses completion when labor hours are still zero. That throws `DomainException`.
6. On success, `SaveChanges` writes the new status. The handler returns the detail DTO, including the transitions that are legal from the new status.
7. If the domain throws, `ApiExceptionHandler` writes a 422 problem document. The controller does not contain that mapping.

That last point is the one to emphasize: status codes live in one place, business rules live on the aggregate.

## The work-order state machine

Jobs are not a free-form status string.

- Open can start or be cancelled.
- In progress can wait on parts, complete, or be cancelled.
- Waiting on parts can resume or be cancelled.
- Completed and cancelled are terminal for edits. Notes are still allowed, because the night shift has to write down what they found after the job is closed.

You cannot jump from Open to Completed. You cannot complete with zero hours. Labor is rounded to two decimal places and rejected outside 0 to 999.99.

The detail API returns `allowedTransitions`. The phone renders those as buttons. The rules are not copied into Dart.

Technicians may only change jobs they created or are assigned to, and they may only assign a job to themselves. Supervisors and admins can change any job and are the only roles that can edit the asset register. Those checks are in `AccessRules`, with a unit test for the technician case and an API test that a technician gets 403 trying to move someone else's pump job.

## Sign-in

Passwords are hashed with ASP.NET Core's `PasswordHasher` (PBKDF2). The login handler returns the same "invalid email or password" message for an unknown email, a bad password, and a disabled account, so the endpoint does not reveal which emails exist.

A successful login returns a short-lived JWT (8 hours, a shift) and a refresh token. The refresh token stored in the database is the SHA-256 hash of the token, not the token itself. Refresh rotates: the presented token is revoked and a new one is issued. If a revoked token is presented again, every refresh token for that user is revoked. That is the reuse detection you can describe if someone asks how you would notice a stolen refresh token.

Logout revokes the presented refresh token and is safe to call twice.

The signing key in `appsettings.json` is a development key. A real deployment sets `Jwt__SigningKey` and turns `Seed__Enabled` off.

## Data

Entities keep setters private and expose factories (`WorkOrder.Open`, `Asset.Register`, `Inspection.Record`). EF Core is configured to use those private setters and the comments backing field. Enums are stored as strings (`InProgress`, not `2`) so a SQL reader can understand a row. Instants are converted to UTC `DateTime` before they hit the database. SQLite cannot `ORDER BY` a `DateTimeOffset`, and both providers need to sort work orders and inspections.

PostgreSQL uses the EF migration in `Persistence/Migrations`. SQLite, used by `dotnet run` on a laptop and by the tests, calls `EnsureCreated` against the same model. The migration is the production schema. `EnsureCreated` is the "I do not want Docker just to run a test" path. A Testcontainers test against real PostgreSQL would be the next step if the suite needed to prove a Postgres-specific query. It is not required to trust the model, and it would make CI depend on Docker.

Work order numbers (`WO-00011`) come from a `number_sequences` row that the migration seeds at 1. The demo seed then advances it past the ten seeded jobs. The unique index on the number is the backstop if two requests race. A production system with heavier concurrency would take that update under a transaction or use a database sequence. Worth saying out loud so you do not pretend the counter is distributed-systems grade.

The dashboard loads a narrow projection of work orders and aggregates in memory. That is honest at this size. At fleet scale those counts belong in SQL. The walkthrough is a good place to say you know the difference.

## Errors and logs

Serilog writes one line per request with the message template and the structured properties. Validation errors are 400 with an `errors` map. Missing resources are 404. Forbidden actions are 403. Duplicate asset tags are 409. Unexpected exceptions are logged with the stack and returned as a generic 500. Passwords are not logged.

`/health/live` only checks that the process is up. `/health/ready` runs the EF Core health check.

## How the tests are shaped

Domain tests do not touch a database. They construct a work order and assert the legal edges, the labor rule, and that a closed job still accepts a comment.

Application tests use small fakes for the repositories and the password hasher. They lock the authorization rule and the validation pipeline without HTTP.

API tests use `WebApplicationFactory`. Each test class gets its own SQLite file, the Development seed, and the real JWT middleware. They sign in as Maya, Priya, or Sam and exercise the routes. The dashboard test asserts the seeded picture exactly: 3 open, 2 overdue, 2 critical assets, average labor 3.0 hours, one failed inspection in the last 30 days. If the seed drifts, that test fails on purpose.

## The Flutter app

The client is feature folders under `mobile/lib/features`: auth, dashboard, work orders, assets, inspections. Riverpod holds the session, the API client, and the repositories. go_router sends signed-out users to the login screen and signed-in users to a shell with Board, Work, and Assets.

`SessionController` stores the access token, refresh token, expiry, and the crew member in `shared_preferences`. A production build would move the refresh token to the platform keystore. Preferences were chosen so widget tests and CI do not need a platform channel. That is a real tradeoff, not an oversight.

`MeridianApi` attaches the bearer token. On a 401 it calls `rotate()` once, which posts the refresh token on a separate Dio instance so the retry cannot recurse, then repeats the call. If rotation fails, the session is cleared and the router returns to login.

Lists and the dashboard write the JSON they just received into a `JsonCache`. If Dio reports a connection failure, the repository reads that entry and marks the page `fromCache`. The screen shows a banner: "Showing the last copy saved on this device." Creating or editing a job does not queue offline. A field app that must capture notes in a dead zone would add an outbox. Say that if asked. The current behavior is easier to explain and to test, and the test `offline list falls back to the cached page` locks it in.

The sign-in screen can fill Maya's demo account so someone reviewing the app does not have to type the password.

## What I would build next

If this were going into a control room rather than a portfolio:

- An outbox for offline writes, with idempotency keys on create.
- Optimistic concurrency on the work order, so two technicians do not silently overwrite each other.
- Refresh tokens in the OS keystore, and a shorter access token once the client refresh path is boringly reliable.
- SQL aggregation for the dashboard, and a cursor instead of offset paging once the work-order table is large.
- OpenTelemetry traces beside the Serilog request log, and the signing key only from the environment.
- A PostgreSQL Testcontainers test for the one or two queries that are provider-sensitive.
- Background work for inspection reminders. The domain already stores `nextDueOn`. Nothing sends it yet.

None of those are missing because the model cannot express them. They are scoped out so the part that is here is finished.
