<!--
Sync Impact Report
==================
Version change: 1.1.0 → 1.2.0
Rationale: MINOR bump — Principle VI was materially expanded with a new
EF Core reconstruction-constructor rule, and a new Core Principle (VII.
Infrastructure Layer Implementation) was added with non-negotiable rules for
the backend Infrastructure project. No existing principle was removed or
redefined incompatibly.

Modified principles:
- VI. Domain-Driven Design in the Domain Layer — added rule requiring a
  private, parameterized constructor for EF Core reconstruction on
  Aggregates, Entities, and Value Objects.

Added sections:
- Core Principles: VII. Infrastructure Layer Implementation

Removed sections: N/A

Deferred / TODO placeholders: None.

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
Rationale: A financial/bills-tracking domain requires high confidence in
correctness; test-first development catches regressions before they reach
users' financial data.

### III. Type Safety & Static Analysis
Backend code MUST use C# nullable reference types and enabled compiler
warnings-as-errors for the core domain and API layers. Frontend code MUST
use Angular in strict TypeScript mode (`strict: true`), with no implicit
`any` in new code. Linting (backend analyzers, frontend ESLint) MUST pass
before merge.
Rationale: Strong typing across both .NET and Angular layers catches entire
classes of bugs at compile time rather than in production, and keeps the
codebase navigable as it grows.

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
Rationale: These constraints keep business rules encapsulated inside a rich
domain model instead of leaking into services or persistence code, and keep
the Domain project's structure predictable and consistent as more Aggregates,
Entities, and Value Objects are added.

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

## Technology Stack Requirements

- Backend: .NET 10 (C#). New backend projects/services MUST target .NET 10
  or later; downgrading the target framework requires a constitution
  amendment.
- Frontend: Angular v22. New frontend code MUST use the Angular CLI
  project structure and Angular's standalone component model as the
  default; downgrading the Angular major version requires a constitution
  amendment.
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
- Constitution compliance (Principles I–VII) MUST be considered part of
  code review, not a separate gate.

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

**Version**: 1.2.0 | **Ratified**: 2026-08-29 | **Last Amended**: 2026-08-31
