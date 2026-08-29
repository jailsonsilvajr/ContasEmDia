<!--
Sync Impact Report
==================
Version change: (none, template) → 1.0.0
Rationale: Initial ratification of the project constitution. No prior concrete
version existed (file only contained unfilled template placeholders).

Modified principles: N/A (initial adoption)

Added sections:
- Core Principles: I. API-First Backend/Frontend Separation, II. Test-First
  Development, III. Type Safety & Static Analysis, IV. Secure Handling of
  Financial Data, V. Simplicity & Incremental Delivery
- Technology Stack Requirements
- Development Workflow & Quality Gates
- Governance

Removed sections: N/A (initial adoption)

Deferred / TODO placeholders: None. RATIFICATION_DATE set to the date this
constitution was first adopted (today), since no earlier ratified version
exists.

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

## Development Workflow & Quality Gates

- All changes MUST go through pull request review before merging to the
  main branch; at least one reviewer approval is required.
- Continuous integration MUST run backend tests, frontend tests, and both
  linters/analyzers on every pull request; a red build blocks merge.
- Breaking API changes MUST be called out explicitly in the pull request
  description, including the migration path for frontend consumers.
- Constitution compliance (Principles I–V) MUST be considered part of
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

**Version**: 1.0.0 | **Ratified**: 2026-08-29 | **Last Amended**: 2026-08-29
