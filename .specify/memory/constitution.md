<!--
Sync Impact Report
==================
Version change: 1.4.0 → 1.4.1
Rationale: PATCH bump — clarified the scope of the PT-BR business-rule
exception message rule in Principle VI: it now explicitly reads "every
exception raised... to signal a violated business rule (validation error,
invariant violation, or disallowed operation)" instead of "every validation
exception", removing ambiguity about whether non-"validation"-labeled
business-rule exceptions (e.g., a disallowed operation given current state)
were covered. This is a wording clarification of the rule added in 1.4.0; it
does not change which layer owns business rules (Domain-only, per Principles
VI/XI) nor introduce a new principle.

Modified principles:
- VI. Domain-Driven Design in the Domain Layer — broadened the PT-BR
  exception message rule's wording from "validation exception" to "any
  exception signaling a violated business rule", and updated the rationale
  accordingly.

Added sections: None.

Removed sections: N/A

Deferred / TODO placeholders: None.

Other updates: None.

Templates requiring follow-up review (not modified by this command, listed
for awareness only): .specify/templates/plan-template.md,
.specify/templates/spec-template.md, .specify/templates/tasks-template.md,
.specify/templates/checklist-template.md — these read the constitution at
runtime and need no edits from this command.
-->

# ContasEmDia Constitution

## Core Principles

### I. API-First Backend/Frontend Separation
The backend (.NET 10) MUST expose all functionality through well-defined,
versionable HTTP APIs; the frontend (Angular v22) MUST consume the system
exclusively through those APIs, never through direct database or backend
internal access. Backend and frontend MUST be independently buildable,
testable, and deployable. API contracts (request/response shapes, status
codes, error formats) MUST be documented and treated as the source of truth
for integration between the two projects.
Rationale: A strict contract boundary keeps the .NET backend and Angular
frontend free to evolve, be versioned, and be tested independently, and
prevents hidden coupling that makes changes on one side silently break the
other.

### II. Test-First Development
For non-trivial features and bug fixes, tests MUST be written before
implementation: unit tests for backend business logic and Angular
components/services, and integration or contract tests for API endpoints.
Tests MUST fail before the corresponding implementation is written, then
pass once it is complete (Red-Green-Refactor). Pull requests that add
behavior without accompanying tests MUST justify the omission explicitly.
Frontend unit tests MUST run on Vitest or Jasmine/Karma, whichever is
configured for the project, and MUST meet the project's minimum line
coverage threshold for critical paths. Every UI component MUST conform to
WCAG 2.1 Level AA accessibility guidelines (keyboard operability, semantic
markup, sufficient contrast, ARIA attributes where native semantics are
insufficient); accessibility is verified as part of component review, not
deferred to a later pass.
Rationale: A financial/bills-tracking domain requires high confidence in
correctness; test-first development catches regressions before they reach
users' financial data. Accessibility is a first-class, non-negotiable
requirement because users must be able to manage their finances regardless
of ability.

### III. Type Safety & Static Analysis
Backend code MUST use C# nullable reference types and enabled compiler
warnings-as-errors for the core domain and API layers. Frontend code MUST
use Angular in strict TypeScript mode (`strict: true`), with no explicit or
implicit `any` anywhere in new code. Linting (backend analyzers, frontend
ESLint) MUST pass before merge. ESLint or TypeScript compiler rules MUST
NOT be disabled via inline comments (e.g. `eslint-disable`,
`@ts-ignore`/`@ts-expect-error`) unless the plan document for the change
records an explicit, reviewed justification for the suppression.
Rationale: Strong typing across both .NET and Angular layers catches entire
classes of bugs at compile time rather than in production, and keeps the
codebase navigable as it grows. Requiring justification for lint/type
suppressions prevents silent erosion of these guarantees.

### IV. Secure Handling of Financial Data
Any code that stores, transmits, or displays account, bill, or payment
data MUST treat that data as sensitive by default: secrets and connection
strings MUST NOT be committed to source control; all API endpoints that
expose or mutate user financial data MUST enforce authentication and
authorization; and input validation MUST occur at the API boundary before
data reaches business logic or persistence.
Rationale: ContasEmDia manages users' bills and financial obligations;
mishandled data has direct real-world consequences for users, so security
is a non-negotiable baseline rather than an optional hardening pass.

### V. Simplicity & Incremental Delivery
Features MUST be implemented with the simplest design that satisfies the
current requirement (YAGNI). New abstractions, libraries, or architectural
layers MUST be justified by a concrete, present need — not by anticipated
future requirements. Prefer small, independently reviewable increments
over large, multi-purpose changes.
Rationale: A small full-stack project benefits more from clarity and low
maintenance overhead than from speculative flexibility.

### VI. Domain-Driven Design in the Domain Layer
The backend Domain project MUST model the business domain using DDD tactical
patterns — Aggregates, Entities, Value Objects, and Repository interfaces —
under the following non-negotiable rules:
- Each building block type MUST live in its own dedicated top-level folder in
  the Domain project: `/Aggregates`, `/Entities`, `/ValueObjects`,
  `/Repositories`.
- Repository interfaces MUST be defined only for Aggregates. Entities and
  Value Objects MUST NOT have their own repository; they are reachable only
  through the Aggregate that owns them.
- Aggregates and Entities MUST NOT expose primitive-typed properties (e.g.,
  `int`, `string`, `bool`, `decimal`, `DateTime`). Every property that carries
  domain meaning MUST be represented as a Value Object.
- Aggregates, Entities, and Value Objects MUST be created only through their
  constructors. Parameterless construction, public setters used for
  initialization, or object-initializer-based creation that bypasses
  constructor validation MUST NOT be used.
- Every Aggregate, Entity, and Value Object MUST additionally declare a
  private, parameterized constructor used exclusively by Entity Framework
  Core to reconstruct instances from persisted data. This constructor MUST
  remain private, MUST NOT be called from application or domain code, and
  MUST NOT be used as a way to bypass the invariant validation performed by
  the public constructor(s).
- The identifier of every Aggregate and every Entity MUST be a GUID.
- Properties of Aggregates, Entities, and Value Objects MUST NOT be exposed as
  directly gettable/settable state. All reads and mutations MUST go through
  business-intent methods (e.g., an Account Aggregate MUST NOT expose a
  `Balance` property, but MUST expose methods such as `CalculateBalance()`,
  `Debit()`, and `Credit()`).
- Value Objects MUST be immutable: no internal state may change after
  construction. Every Value Object MUST provide a `GetValue()` method to read
  its underlying value.
- Validation of Aggregate, Entity, and Value Object invariants MUST be
  implemented by hand inside constructors and business methods. Validation
  libraries such as FluentValidation MUST NOT be used in the Domain layer.
- The Domain layer MUST NOT implement Domain Events. Cross-aggregate or
  cross-context notification mechanisms are out of scope until a future
  amendment explicitly authorizes them.
- Every exception raised by an Aggregate, Entity, or Value Object
  constructor or business method to signal a violated business rule — a
  validation error, an invariant violation, or a disallowed operation —
  MUST carry a user-facing message in Brazilian Portuguese (PT-BR), scoped
  to the specific field or rule that was violated. This applies to every
  business-rule exception without exception, not only to what might
  narrowly be labeled "validation". Outer layers (Application, API) MUST
  relay this message as-is when reporting the failure to the caller; they
  MUST NOT maintain a separate, duplicated set of messages or
  translate/rewrite the Domain's message.
Rationale: These constraints keep business rules encapsulated inside a rich
domain model instead of leaking into services or persistence code, and keep
the Domain project's structure predictable and consistent as more Aggregates,
Entities, and Value Objects are added. Requiring the Domain itself to own the
PT-BR message text for every business-rule exception — not just validation
errors — keeps a single source of truth for user-facing wording next to the
rule it describes, instead of scattering translated or duplicated copies
across outer layers.

### VII. Infrastructure Layer Implementation
The backend Infrastructure project MUST provide the persistence
implementation for the Domain layer under the following non-negotiable
rules:
- The Infrastructure project MUST implement every Repository interface
  defined in the Domain project's `/Repositories` folder; no Domain
  repository interface may be left without a concrete implementation.
- The Infrastructure project MUST organize its content into, at minimum,
  the following top-level folders: `/Repositories` (repository
  implementations), `/Migrations` (EF Core migrations), `/Configs` (EF Core
  entity and relationship configuration classes), and `/Contexts` (DbContext
  classes). Additional folders MAY be added as needed.
- EF Core migrations belonging to previously delivered features MUST NOT be
  altered, renamed, or deleted. Schema changes for new work MUST be
  introduced as new migrations.
- The Infrastructure project MUST use Entity Framework Core 10.
- The Infrastructure layer MUST NOT implement an explicit Unit of Work
  abstraction. `DbContext.SaveChangesAsync()` IS the project's Unit of Work,
  and callers MUST invoke it directly rather than through a custom wrapper.
- Repository access MUST be provided through a single `RepositoryManager`
  that exposes each repository as a `Lazy<T>`-backed property, so that a
  given repository is instantiated only on first use.
Rationale: These constraints keep persistence concerns isolated behind the
Domain-defined repository contracts, prevent migration history for shipped
features from being rewritten, and avoid duplicating EF Core's own
transaction/unit-of-work semantics with a redundant abstraction.

### VIII. Angular Standalone Architecture & Project Structure
Frontend code MUST use Angular's standalone component model exclusively;
traditional `NgModule`-based components, directives, or pipes MUST NOT be
introduced in new code. The frontend MUST be organized by feature/domain
folders rather than by technical layer (e.g. a `contas-a-pagar/` feature
folder holding its own components, services, and models, not a global
`components/` folder mixed across features). Styling MUST use Tailwind CSS
utility classes for global and component-level styling; encapsulated SCSS
MAY be used instead for a specific component when Tailwind utilities are
demonstrably insufficient, with the reason noted in the component's plan.
Rationale: Standalone components remove `NgModule` boilerplate and make
each feature's dependencies explicit and traceable. Feature-based
organization keeps a growing frontend navigable and lets features be
reasoned about, tested, and removed independently. A single default
styling approach (Tailwind) avoids fragmented, hard-to-maintain CSS.

### IX. Signal-Based Reactivity & HTTP Access
Local component/service state and derived state MUST be managed with
Angular Signals (`signal`, `computed`, `effect`) rather than RxJS
`BehaviorSubject`/manual subscription management. RxJS MAY be used where it
is genuinely the better fit — complex asynchronous event flows such as
HTTP request orchestration, debouncing/throttling user input, or
WebSocket streams — but MUST NOT be used as a substitute for local state
that Signals can express directly. `HttpClient` MUST be injected via the
`inject()` function and used from `providedIn: 'root'` or feature-scoped
services, never instantiated or accessed directly from components.
Rationale: Signals give Angular fine-grained, push-based reactivity with
less boilerplate and easier debugging than manual RxJS subscription
management for local state, while RxJS remains the right tool for genuinely
asynchronous, multi-event streams.

### X. Dependency Injection & Frontend Coding Standards
All Angular dependency injection MUST use the `inject(Service)` function
form; constructor-parameter injection MUST NOT be used in new code. File
naming MUST follow Angular convention by suffix: components as
`*.component.ts`, services as `*.service.ts`, directives as
`*.directive.ts`, and pipes as `*.pipe.ts`. Component `input()` values and
signal-managed internal state MUST be treated as immutable: consumers MUST
NOT mutate an input's referenced object/array in place, and updates to
signal state MUST go through `set`/`update` with new values rather than
in-place mutation of the held value.
Rationale: A single, consistent DI and naming convention makes the
codebase predictable to navigate and review. Treating inputs and signal
state as immutable prevents a common class of Angular change-detection
bugs where a mutated reference does not trigger the reactivity Angular (or
`OnPush`) is expecting.

### XI. Application Layer Implementation
The backend Application project MUST orchestrate use cases on top of the
Domain layer under the following non-negotiable rules:
- Each UseCase MUST be organized in its own dedicated folder within the
  Application project (e.g. `/UseCases/<NomeDoUseCase>/`), grouping that
  UseCase's interface, implementation, Input, and Output together.
- Every UseCase MUST have an interface. Interfaces MUST be named
  `I<Nome>UseCase` and implementations MUST be named `<Nome>UseCase`
  (e.g. `ICreateAccountUseCase` / `CreateAccountUseCase`).
- Every UseCase MUST define its own Input and Output types, scoped to that
  UseCase only; Input/Output types MUST NOT be shared or reused across
  different UseCases. They MUST be named `<NomeDoUseCase>Input` and
  `<NomeDoUseCase>Output` (e.g. `CreateAccountUseCaseInput` /
  `CreateAccountUseCaseOutput`).
- UseCases MUST only orchestrate: at minimum, fetch the relevant
  Aggregate(s) from the repository, invoke the appropriate business
  method(s) on the Aggregate to perform the operation, and finish by
  calling `SaveChangesAsync()`. UseCases MUST NOT implement business rules
  or invariant validation themselves — that logic belongs exclusively in
  the Domain layer's Aggregates, Entities, and Value Objects (Principle
  VI).
- The Application project MUST depend only on the Domain project. It
  MUST NOT reference the Infrastructure project or any other outer-layer
  project directly; Infrastructure implementations MUST be supplied to
  Application through Domain-defined abstractions (e.g. repository
  interfaces), resolved via dependency injection at composition time.
Rationale: A folder-per-UseCase structure with a consistent
interface/implementation and Input/Output naming convention keeps a
growing Application layer predictable to navigate. Restricting UseCases to
orchestration keeps business rules concentrated in the rich Domain model
(Principle VI) rather than leaking into application services, and a
Domain-only dependency keeps the Application layer free of persistence or
infrastructure concerns, preserving the direction of dependency from outer
layers inward.

## Technology Stack Requirements

- Backend: .NET 10 (C#). New backend projects/services MUST target .NET 10
  or later; downgrading the target framework requires a constitution
  amendment.
- Frontend: Angular v22. New frontend code MUST use the Angular CLI
  project structure and Angular's standalone component model exclusively
  (see Principle VIII); downgrading the Angular major version requires a
  constitution amendment.
- Frontend styling: Tailwind CSS is the default styling technology (see
  Principle VIII).
- Communication between frontend and backend MUST use HTTP(S) APIs
  (REST and/or well-defined RPC), following Principle I.
- Dependency upgrades within the same major version (e.g., .NET 10.x,
  Angular 22.x) are routine maintenance and do NOT require a constitution
  amendment; changes to the major backend or frontend platform version do.
- Persistence: Entity Framework Core 10, per Principle VII. Downgrading the
  EF Core major version requires a constitution amendment.

## Development Workflow & Quality Gates

- All changes MUST go through pull request review before merging to the
  main branch; at least one reviewer approval is required.
- Continuous integration MUST run backend tests, frontend tests, and both
  linters/analyzers on every pull request; a red build blocks merge.
- Breaking API changes MUST be called out explicitly in the pull request
  description, including the migration path for frontend consumers.
- Constitution compliance (Principles I–XI) MUST be considered part of
  code review, not a separate gate.

## AI Agent Guardrails

- An AI coding agent MUST NOT use `any` (explicit or implicit) or disable
  an ESLint/TypeScript rule via an inline suppression comment unless the
  suppression is justified in the technical plan for the change (see
  Principle III); an unjustified suppression MUST be treated as a defect
  to fix, not merged as-is.
- An AI coding agent MUST NOT introduce a new external npm dependency
  without first updating `plan.md` to name the dependency and its
  justification, and obtaining explicit human validation of that plan
  update; adding a dependency directly to `package.json` without this step
  is prohibited.
Rationale: AI agents can otherwise silently widen the codebase's type-safety
surface or its dependency footprint faster than a human reviewer would
normally allow; routing both decisions through the plan document keeps a
human in the loop before either happens.

## Governance

This constitution supersedes any conflicting practice or informal
convention within the project. Amendments are made by editing this file
via the `/speckit-constitution` workflow and MUST include: the specific
change, a rationale, and the resulting version bump.

Versioning policy (semantic versioning applied to governance):
- MAJOR: Backward-incompatible removal or redefinition of a principle or
  governance rule (e.g., dropping the API-first boundary, changing the
  mandated backend/frontend platform).
- MINOR: Adding a new principle or materially expanding existing guidance.
- PATCH: Clarifications, wording fixes, or non-semantic refinements.

All pull requests and design reviews MUST verify compliance with this
constitution. Any deviation MUST be justified in the pull request or
design document and, if it reveals a gap in this document, MUST be
followed by a proposed amendment. Complexity that violates Principle V
(Simplicity & Incremental Delivery) MUST be explicitly justified before
approval.

**Version**: 1.4.1 | **Ratified**: 2026-08-29 | **Last Amended**: 2026-09-04
