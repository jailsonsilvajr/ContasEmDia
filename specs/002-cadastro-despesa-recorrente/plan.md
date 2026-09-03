# Implementation Plan: Cadastro de Despesa Recorrente (Frontend)

**Branch**: `002-cadastro-despesa-recorrente` | **Date**: 2026-09-02 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/002-cadastro-despesa-recorrente/spec.md`

## Summary

Build the Angular v22 "Nova despesa recorrente" screen: a standalone,
signal-driven form (name, category, monthly amount, due day, start date,
frequency fixed to Monthly, status, optional note) with a live-updating
preview card, inline per-field validation revealed on blur or submit
attempt, and idle/loading/success/error states around a single
`POST /api/recurring-expenses` call. The technical approach follows the
component/state/contract design already worked out in
[`refinements/frontend/cadastro-despesa-recorrente.md`](../../refinements/frontend/cadastro-despesa-recorrente.md):
two standalone components (`CadastroDespesaRecorrenteComponent` — smart,
owns form state via Signals; `DespesaPreviewComponent` — presentational,
`input()`-only), one `HttpClient`-based service, and client-only DTOs/enums
mirroring the backend domain model without duplicating its validation
authority. The Nome field's 100-character cap is enforced both natively
(`maxlength="100"` on the input, per the spec's Clarification #5) and by
the existing reactive validation, which stays as a backstop — see
[research.md](research.md) §10. No backend work is in scope — only the Domain project exists
today (`backend/Domain`, see
[`specs/001-despesa-recorrente-domain`](../001-despesa-recorrente-domain/));
this feature's target endpoint is documented as a contract for a future
backend feature (see [`contracts/api-contract.md`](contracts/api-contract.md)).

**Plan update (design-fidelity pass)**: the spec's later Clarifications
(#6–#9) and FR-018/FR-019/SC-006 require the screen to reproduce
`design/Cadastro.dc.html` with strict visual fidelity (exact colors,
Sora/Public Sans typography, spacing, border-radius) through Tailwind
utilities/tokens — never by copying the design file's inline styles — with
the accent color sourced from one shared theme token (not duplicated in this
feature), the design's own logo/header excluded as out of scope, and the
layout usable down to 360px-wide viewports. See
[research.md](research.md) §11–§13 for how this is achieved; the current
implementation (generic `blue-600`/`red-600`/`slate-*` Tailwind classes, no
`maxlength`, no Sora/Public Sans, no shared accent token — see `frontend/src/app/features/despesa-recorrente/cadastro-despesa-recorrente/cadastro-despesa-recorrente.component.html`)
predates this pass and is now out of sync with the spec; reconciling it is
implementation work for a future `/speckit-tasks` + `/speckit-implement`
pass, not this plan update itself.

## Technical Context

**Language/Version**: TypeScript on Angular v22 (Constitution: Technology
Stack Requirements)

**Primary Dependencies**: `@angular/core`/`common`/`platform-browser` (via
`ng new` scaffolding — no `@angular/forms`/`ReactiveFormsModule` needed,
since form state is plain Signals bound through `(input)`/`(change)`/
`(blur)`, not `FormGroup`); Tailwind CSS (`tailwindcss`,
`@tailwindcss/postcss`, `postcss`) for styling (Constitution Principle
VII) — this is the one **new** npm dependency this feature introduces; see
[research.md](research.md) §7 for the AI Agent Guardrails justification
this plan update satisfies.

**Storage**: N/A — this screen only calls a single (not-yet-implemented)
HTTP endpoint; no client-side persistence

**Testing**: Vitest, the Angular CLI's current default test runner for new
projects (Constitution Principle II allows "Vitest or Jasmine/Karma,
whichever is configured for the project" — see [research.md](research.md)
§2); component/service tests use `HttpTestingController` to simulate every
server response shape, since no live backend exists yet

**Target Platform**: Browser (Angular SPA), served by the Angular CLI dev
server / static build — no SSR requirement in this feature

**Project Type**: Frontend-only screen — no backend work in this feature
(Web application, "Option 2" shape: new `frontend/` project alongside the
existing `backend/`)

**Performance Goals**: Preview updates MUST be synchronous with typing —
same render cycle, no debounce (SC-003); no other throughput/latency target
applies to a single-screen form

**Constraints**: No client-side submission timeout (spec Clarification
#3) — `formStatus` stays `loading` until the real HTTP response (success or
failure) arrives; category set and Monthly-only frequency are hardcoded on
the client (spec Assumptions); duplicate submits MUST be prevented while a
submission is in flight (FR-013)

**Scale/Scope**: 2 standalone components, 1 service, 1 model file, 1
external endpoint dependency — see [data-model.md](data-model.md)

**Design Fidelity**: `design/Cadastro.dc.html` is the binding visual/
interaction reference (FR-018) — colors, Sora (headings/amount)/Public Sans
(body) typography, spacing and border-radius MUST be reproduced through
Tailwind utility classes/tokens, never by copying the file's inline styles.
The accent color (`#2E6FF2` in the design's `accent` prop) MUST resolve from
one Tailwind theme token defined once in the shared `frontend/src/styles.css`
(a `@theme` block, Tailwind v4's CSS-first token mechanism), not hardcoded
inside this feature's components — see [research.md](research.md) §11. The
design's top page header (logo/icon + "ContasEmDia") is explicitly out of
scope (spec Clarification #7) — this feature's component tree starts at the
"Nova despesa recorrente" `<h1>`, see [research.md](research.md) §13. The
layout MUST stay usable (no horizontal scroll, all controls reachable and
legible, WCAG 2.1 AA) from 360px viewport width up (FR-019, SC-006), beyond
the design's own wide-viewport-only `flex-wrap` behavior — see
[research.md](research.md) §12.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applicability | Assessment |
|---|---|---|
| I. API-First Backend/Frontend Separation | Applies | The screen consumes the system exclusively through one documented HTTP endpoint ([contracts/api-contract.md](contracts/api-contract.md)), via `HttpClient`, never direct backend/DB access. The endpoint itself isn't implemented yet — this is a forward-looking contract (same pattern the Domain feature already establishes for the layer above it), not a boundary violation. |
| II. Test-First Development | Applies | Vitest component/service tests (using `HttpTestingController`) MUST be written before/alongside each behavior and MUST fail before the implementation exists, enforced during `/speckit-tasks` + `/speckit-implement`. Every UI element MUST meet WCAG 2.1 AA (labelled inputs, `aria-invalid`/`aria-describedby` wiring error messages to fields, sufficient contrast on error/success states, full keyboard operability of the segmented status toggle and buttons). |
| III. Type Safety & Static Analysis | Applies | `tsconfig` MUST keep Angular's `strict: true`; no explicit/implicit `any` in `despesa-recorrente.model.ts` or components; ESLint MUST pass with no unjustified inline suppressions. |
| IV. Secure Handling of Financial Data | Mostly N/A at this layer | No secrets/connection strings here; authentication for the `POST` call is assumed already handled by existing frontend infrastructure (spec Assumptions), out of this screen's scope. Input validation happens client-side for UX only — the backend remains the authority (spec Assumptions, refinement doc), so no validation is "trusted" past the API boundary. |
| V. Simplicity & Incremental Delivery | Applies | Two components + one service + one model file; no state-management library, no date/money library, no `ReactiveFormsModule` — Signals and hand-rolled parsing/formatting are sufficient for this screen's scope (see [research.md](research.md) §3–4). |
| VI. Domain-Driven Design in the Domain Layer | N/A this feature | No backend Domain-layer code is touched; this feature is frontend-only. |
| VII. Angular Standalone Architecture & Project Structure | Applies — core gate | Both components are standalone; the feature lives under its own `features/despesa-recorrente/` folder (not a shared `components/` folder); styling uses Tailwind utility classes exclusively (no SCSS escape hatch needed for this screen). Design-fidelity (FR-018) is achieved with Tailwind tokens, not inline styles copied from `design/Cadastro.dc.html`; the accent color and Sora/Public Sans font-family tokens live once in the shared `frontend/src/styles.css` `@theme` block (app-wide, not feature-local), since this is the first feature to establish it — see [research.md](research.md) §11. |
| VIII. Signal-Based Reactivity & HTTP Access | Applies — core gate | All local/derived form state uses `signal`/`computed` (see [data-model.md](data-model.md)), not `BehaviorSubject`; `HttpClient` is injected via `inject()` inside `DespesaRecorrenteService`, never instantiated or accessed directly from a component. |
| IX. Dependency Injection & Frontend Coding Standards | Applies | `inject()` form used throughout (no constructor-parameter injection); files follow `*.component.ts`/`*.service.ts` naming; `input()` values on `DespesaPreviewComponent` and all signal state are only ever replaced via `set`/`update`, never mutated in place. |

**Result**: PASS. No violations requiring justification beyond the
AI Agent Guardrails dependency call-out below — Complexity Tracking table
is not needed.

### New dependency requiring explicit human validation (AI Agent Guardrails)

Per the constitution's AI Agent Guardrails, adding a new external npm
dependency requires naming it and its justification in this plan and
obtaining explicit human validation of this update before it is added to
`package.json`:

- **Tailwind CSS** (`tailwindcss`, `@tailwindcss/postcss`, `postcss`) —
  justification: Constitution Principle VII mandates Tailwind as the
  project's default frontend styling technology; this is the first feature
  to scaffold the frontend workspace, so it is also the first to add this
  already-mandated dependency. See [research.md](research.md) §7.
- **`@angular-eslint/schematics`** (and the `eslint`/`typescript-eslint`
  dev dependencies it installs via `ng add`) — justification: discovered
  during implementation that the Angular v22 CLI's `ng new` no longer
  scaffolds any ESLint config or `lint` architect target by default, which
  the original plan assumed. Constitution Principle III mandates ESLint;
  `ng add @angular-eslint/schematics` is the standard, CLI-supported way to
  add it to an existing workspace. See [research.md](research.md) §8.
- **`@vitest/coverage-v8`** (dev dependency) — justification: Constitution
  Principle II requires frontend unit tests to "meet the project's minimum
  line coverage threshold," which requires a coverage reporter; the CLI's
  `@angular/build:unit-test` Vitest integration does not bundle one by
  default. `@vitest/coverage-v8` is Vitest's standard first-party V8-based
  coverage provider. See [research.md](research.md) §9.

No other new dependency is introduced (no `ReactiveFormsModule`, no date or
money-formatting library — see [research.md](research.md) §3–4).

## Project Structure

### Documentation (this feature)

```text
specs/002-cadastro-despesa-recorrente/
├── plan.md               # This file (/speckit-plan command output)
├── research.md           # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md         # Phase 1 output (/speckit-plan command)
├── contracts/
│   └── api-contract.md   # Phase 1 output (/speckit-plan command)
└── tasks.md              # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
frontend/
├── angular.json
├── package.json
├── src/
│   ├── styles.css                                  # @import "tailwindcss"; + @theme block:
│   │                                                #   shared accent color + Sora/Public
│   │                                                #   Sans font tokens, Google Fonts
│   │                                                #   @import (see research.md §11)
│   └── app/
│       └── features/
│           └── despesa-recorrente/
│               ├── cadastro-despesa-recorrente/
│               │   ├── cadastro-despesa-recorrente.component.ts    (standalone, smart)
│               │   └── cadastro-despesa-recorrente.component.html
│               ├── despesa-preview/
│               │   ├── despesa-preview.component.ts                (standalone, presentational)
│               │   └── despesa-preview.component.html
│               ├── despesa-recorrente.service.ts   # HttpClient via inject()
│               └── despesa-recorrente.model.ts     # DTOs, CategoryValue, CATEGORY_OPTIONS/COLORS
└── (test files colocated per Angular CLI convention: *.spec.ts next to
    each .ts file above, run by Vitest)
```

**Structure Decision**: Web-application shape (template Option 2), adapted
to this repo's existing `backend/`-per-project-folder convention: a new
top-level `frontend/` Angular CLI workspace, organized internally by
feature per Constitution Principle VII
(`frontend/src/app/features/despesa-recorrente/`) rather than by technical
layer. This is the only frontend code this feature introduces — no other
feature folder, shared component library, or routing is created, since this
screen is the entire scope (spec Assumptions: listing/edit/pause/delete and
the monthly panel are explicitly out of scope).

## Complexity Tracking

*No entries — Constitution Check reported no violations.* The one
dependency addition (Tailwind CSS) is a constitution-mandated default, not
a violation, and is called out above per the AI Agent Guardrails rather
than in this table.
