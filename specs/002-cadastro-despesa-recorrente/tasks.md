---

description: "Task list template for feature implementation"
---

# Tasks: Cadastro de Despesa Recorrente (Frontend)

**Input**: Design documents from `/specs/002-cadastro-despesa-recorrente/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-contract.md, quickstart.md

**Tests**: Included and REQUIRED — Constitution Principle II (Test-First Development) mandates tests before implementation for non-trivial frontend behavior, and plan.md commits to Vitest + `HttpTestingController` component/service tests written before/alongside each behavior.

**Organization**: Tasks are grouped by user story (spec.md priorities P1–P3) to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- File paths are relative to the repository root unless noted otherwise

## Path Conventions

All frontend code lives under `frontend/src/app/features/despesa-recorrente/` per plan.md's Structure Decision (Constitution Principle VII). Test files are colocated `*.spec.ts` files run by Vitest.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Scaffold the (currently nonexistent) Angular v22 `frontend/` workspace and its styling/lint tooling.

- [X] T001 Scaffold the Angular v22 workspace at `frontend/` via `npx @angular/cli new frontend --directory=frontend --style=css --routing=false --skip-git`; confirm `testRunner: vitest` in the generated `angular.json` (research.md §2)
- [X] T002 Install Tailwind CSS dev dependencies (`tailwindcss`, `@tailwindcss/postcss`, `postcss`) in `frontend/package.json` — the one new dependency validated in plan.md's AI Agent Guardrails section (research.md §7)
- [X] T003 [P] Configure Tailwind CSS: create `frontend/.postcssrc.json` with the `@tailwindcss/postcss` plugin and replace the contents of `frontend/src/styles.css` with `@import "tailwindcss";`
- [X] T004 [P] Confirm the generated frontend ESLint config enforces strict TypeScript (`strict: true`, no explicit/implicit `any`) per Constitution Principle III, adjusting `frontend/tsconfig.json`/`frontend/eslint.config.js` if the CLI defaults fall short — CLI generated no ESLint config at all; added via `ng add @angular-eslint/schematics` (research.md §8) plus explicit `strict: true` in `tsconfig.json` and `@typescript-eslint/no-explicit-any: 'error'`
- [X] T005 Create the feature folder structure `frontend/src/app/features/despesa-recorrente/cadastro-despesa-recorrente/` and `frontend/src/app/features/despesa-recorrente/despesa-preview/` per plan.md's Project Structure

**Checkpoint**: `cd frontend && npm run build` and `npm test` both succeed on the empty scaffold before any feature code is added.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared types, HTTP wiring, and the service every user story's implementation depends on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T006 [P] Create `despesa-recorrente.model.ts` in `frontend/src/app/features/despesa-recorrente/despesa-recorrente.model.ts` with `CategoryValue`, `StatusValue`, `FormStatus` types; `CategoryOption` interface; `CATEGORY_OPTIONS`; `CATEGORY_COLORS`; `CreateRecurringExpenseRequest`; `OccurrenceResponse`; `CreateRecurringExpenseResponse`; `FieldError`; `ApiErrorResponse` (data-model.md "Types & DTOs")
- [X] T007 Configure `provideHttpClient()` in `frontend/src/app/app.config.ts` so `HttpClient` is available for injection (Constitution Principle VIII)
- [X] T008 [P] Write a failing test for `DespesaRecorrenteService.create()` in `frontend/src/app/features/despesa-recorrente/despesa-recorrente.service.spec.ts`, using `HttpTestingController` to assert a `POST /api/recurring-expenses` request with the expected `CreateRecurringExpenseRequest` body, and to simulate `201`, `400`, and network-failure responses (research.md §6)
- [X] T009 Implement `DespesaRecorrenteService` in `frontend/src/app/features/despesa-recorrente/despesa-recorrente.service.ts`: `create(payload: CreateRecurringExpenseRequest): Observable<CreateRecurringExpenseResponse>`, `HttpClient` injected via `inject()`, `providedIn: 'root'`, no client-side retry/timeout logic (data-model.md "Service"), making T008 pass

**Checkpoint**: Foundation ready — model types and the HTTP service exist and are tested; user story implementation can now begin.

---

## Phase 3: User Story 1 - Cadastrar uma nova despesa recorrente (Priority: P1) 🎯 MVP

**Goal**: User fills the form with valid data, submits, and the system confirms creation by name; frequency defaults to Monthly and status defaults to Active when left untouched.

**Independent Test**: Fill all required fields with valid values, click "Salvar despesa", verify the system confirms creation by displaying the registered name.

### Tests for User Story 1

> **Write these tests FIRST, ensure they FAIL before implementation.**

- [X] T010 [P] [US1] Component test in `frontend/src/app/features/despesa-recorrente/cadastro-despesa-recorrente/cadastro-despesa-recorrente.component.spec.ts`: filling all required fields and clicking "Salvar despesa" sends exactly one `POST` with the expected `CreateRecurringExpenseRequest`; flushing a `201` shows the success confirmation with the submitted name (US1-1, spec Acceptance Scenario 1)
- [X] T011 [P] [US1] Component test (same file): clicking "Cadastrar outra despesa" after a successful save resets every signal to its initial value and `formStatus()` returns to `'idle'` (US1-2, FR-014)
- [X] T012 [P] [US1] Component test (same file): leaving frequency and status untouched produces a submitted payload with `frequency: 'Monthly'` and `status: 'Active'` (US1-3, US1-4)

### Implementation for User Story 1

- [X] T013 [US1] Create the standalone `CadastroDespesaRecorrenteComponent` skeleton in `frontend/src/app/features/despesa-recorrente/cadastro-despesa-recorrente/cadastro-despesa-recorrente.component.ts` with signals `nome`, `categoria`, `valor`, `dia`, `dataInicio`, `status`, `observacao`, `formStatus`, `savedName`, `submitErrorMessage` at their documented initial values (data-model.md "Component state"), injecting `DespesaRecorrenteService` via `inject()`
- [X] T014 [US1] Implement `onSalvar()` in the same component: build a `CreateRecurringExpenseRequest` from current signal values, call `despesaRecorrenteService.create()`, transition `formStatus` `idle`→`loading`→`success`/`error`, and guard against duplicate in-flight submissions (FR-013, data-model.md "State machine") — makes T010/T012 pass
- [X] T015 [US1] Implement `onNovaDespesa()` in the same component to reset every signal listed in T013 back to its initial value (FR-014) — makes T011 pass
- [X] T016 [US1] Build `cadastro-despesa-recorrente.component.html`: labeled inputs for nome, categoria (`<select>` from `CATEGORY_OPTIONS`), valor, dia, data de início, a disabled frequency control fixed to "Mensal", a status toggle (Ativa/Pausada), an optional observação textarea, the "Salvar despesa" button, and a success view showing `savedName()` with a "Cadastrar outra despesa" button
- [X] T017 [US1] Bootstrap `CadastroDespesaRecorrenteComponent` as the app's rendered screen — actual scaffolded root files are `frontend/src/app/app.ts`/`app.html` (this Angular CLI version no longer generates `app.component.ts`, unlike our own `*.component.ts` files), since `--routing=false` means this is the entire app shell

**Checkpoint**: User Story 1 is fully functional and independently testable — a user can register a recurring expense end-to-end against a simulated backend.

---

## Phase 4: User Story 2 - Acompanhar uma pré-visualização em tempo real (Priority: P2)

**Goal**: A preview card reflects every form change immediately (name, category color, formatted currency, due-day label, status helper text), with no separate "update" action.

**Independent Test**: Fill/change each form field and verify the preview card updates the corresponding value immediately.

### Tests for User Story 2

- [X] T018 [P] [US2] Component test in `cadastro-despesa-recorrente.component.spec.ts`: typing into `nome`/selecting `categoria`/typing `valor`/typing `dia`/toggling `status` updates `nomePreview()`, `categoriaLabel()`+`catColor()`, `valorFmt()`, `diaLabel()`, `statusHelperLabel()` synchronously in the same tick — no `fakeAsync`/`tick()` needed (US2-1..5, SC-003)
- [X] T019 [P] [US2] Component test in `frontend/src/app/features/despesa-recorrente/despesa-preview/despesa-preview.component.spec.ts`: `DespesaPreviewComponent` renders exactly whatever is passed via its `input()`s, with no logic of its own

### Implementation for User Story 2

- [X] T020 [US2] Add computed signals `nomePreview`, `categoriaLabel`, `catColor`, `valorNum`, `valorFmt`, `diaLabel`, `statusHelperLabel`, `isAtiva`, `isPausada` to `cadastro-despesa-recorrente.component.ts` per the rules in data-model.md "Derived state" — makes T018 pass
- [X] T021 [P] [US2] Create the standalone, presentational `DespesaPreviewComponent` in `frontend/src/app/features/despesa-recorrente/despesa-preview/despesa-preview.component.ts` with `input()`s for `nome`, `categoriaLabel`, `catColor`, `valorFmt`, `diaLabel`, `statusHelperLabel` (data-model.md "Presentational component") — makes T019 pass
- [X] T022 [P] [US2] Build `despesa-preview.component.html` rendering the six inputs from T021 (name, category color/label, formatted currency, "Dia X"/"Dia --", status helper text)
- [X] T023 [US2] Wire `<app-despesa-preview>` into `cadastro-despesa-recorrente.component.html`, binding its inputs to the computed signals from T020

**Checkpoint**: User Stories 1 and 2 both work independently — the preview card is live while the form remains fully submittable.

---

## Phase 5: User Story 3 - Ser impedido de salvar dados inválidos (Priority: P2)

**Goal**: Required-field errors are revealed on blur or submit attempt, submission is blocked while any required field is invalid, and all invalid fields are highlighted at once with a corrective notice.

**Independent Test**: Leave a required field empty or invalid, attempt to save, and verify the system does not complete the registration and visually flags the problematic field(s).

### Tests for User Story 3

- [X] T024 [P] [US3] Component test in `cadastro-despesa-recorrente.component.spec.ts`: empty `nome` shows the required-field message and `nome` > 100 characters shows the max-length message, in both cases on blur or on submit (`showNomeError()`) (US3-1, US3-2, FR-002)
- [X] T025 [P] [US3] Component test (same file): `valor` that is zero, negative, or has more than two decimal places shows the corresponding message on blur or submit (`showValorError()`) (US3-3, FR-004)
- [X] T026 [P] [US3] Component test (same file): `dia` outside `1..31` shows the corresponding message on blur or submit (`showDiaError()`) (US3-4, FR-005)
- [X] T027 [P] [US3] Component test (same file): an invalid `dataInicio` shows the corresponding message on blur or submit (`showDataInicioError()`) (US3-5, FR-006)
- [X] T028 [P] [US3] Component test (same file): submitting with multiple invalid fields reveals every relevant `show*Error()` at once, shows the generic "corrija os campos" notice, and `HttpTestingController.expectNone(...)` confirms no request was sent (US3-6, FR-011, SC-002)

### Implementation for User Story 3

- [X] T029 [US3] Add `touched` and `submitAttempted` signals to `cadastro-despesa-recorrente.component.ts` at their documented initial values (data-model.md "Component state")
- [X] T030 [US3] Implement computed signals `nomeError`, `valorError`, `diaError`, `dataInicioError`, and `isFormValid` in the same component per the rules in data-model.md "Derived state" (FR-002, FR-004, FR-005, FR-006)
- [X] T031 [US3] Implement computed signals `showNomeError`, `showValorError`, `showDiaError`, `showDataInicioError` gating message visibility on `(touched().<campo> || submitAttempted()) && <campo>Error() !== null` (FR-012) — makes T024–T027 pass
- [X] T032 [US3] Update `onSalvar()` (from T014) to check `isFormValid()` first: when invalid, set `submitAttempted(true)`, surface the generic "corrija os campos" notice, and return without calling the service (FR-011) — makes T028 pass
- [X] T033 [US3] Update `cadastro-despesa-recorrente.component.html`: wire `(blur)` handlers to update `touched`, render each field's inline error message with `aria-invalid`/`aria-describedby` wiring it to the field (Constitution Principle II — WCAG 2.1 AA), and render the generic "corrija os campos" banner when `submitAttempted()` and `!isFormValid()`

**Checkpoint**: User Stories 1–3 all work independently — invalid submissions are fully blocked and clearly signaled.

---

## Phase 6: User Story 4 - Recuperar-se de uma falha ao salvar (Priority: P3)

**Goal**: A failed save (network/server) shows an error, retains all entered data, and lets the user retry with a single click; a known-field business-rule rejection shows inline on that field.

**Independent Test**: Simulate a failure on a valid form submission and verify the system shows an error warning, keeps the filled-in data, and allows retrying the save with a single click.

### Tests for User Story 4

- [X] T034 [P] [US4] Component test in `cadastro-despesa-recorrente.component.spec.ts`: flushing a network/`5xx` failure on a valid submit sets `formStatus() === 'error'`, shows the generic banner, and retains every field signal's value (US4-1, FR-015, SC-004)
- [X] T035 [P] [US4] Component test (same file): clicking "Tentar novamente" after an error sends a second identical `POST` with the same payload, with no re-entry of data required (US4-2, FR-017)
- [X] T036 [P] [US4] Component test (same file): flushing a `400` with `errors: [{ field: 'name', message: '...' }]` populates the `nome` field's inline error from the response, in addition to (or instead of) the generic banner (US4-3, FR-016)

### Implementation for User Story 4

- [X] T037 [US4] Extend `onSalvar()`'s error handler in `cadastro-despesa-recorrente.component.ts` to map each `ApiErrorResponse.errors[].field` (`name`, `category`, `monthlyAmount`, `dueDay`, `startDate`) to its matching client field's error state; any unmatched field or non-`400`/network failure falls back to `submitErrorMessage` (research.md §5, FR-016) — makes T034/T036 pass
- [X] T038 [US4] Implement the "Tentar novamente" retry action to re-invoke `onSalvar()` with the current (unmodified) signal values, without resetting the form (FR-017) — makes T035 pass
- [X] T039 [US4] Update `cadastro-despesa-recorrente.component.html`: render the error banner with a "Tentar novamente" button when `formStatus() === 'error'`, and route backend field errors from T037 through the same inline error rendering built in T033

**Checkpoint**: All four user stories are independently functional — the screen fully satisfies spec.md.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Verification passes that span every user story.

- [X] T040 [P] Run ESLint across `frontend/src/app/features/despesa-recorrente/` and confirm it passes with zero inline suppressions (Constitution Principle III / AI Agent Guardrails)
- [X] T041 [P] Run `cd frontend && npm run build` and confirm a clean strict-mode TypeScript build with no `any` in `despesa-recorrente.model.ts` or either component (Constitution Principle III) — also removed `$any()` template casts in favor of typed `(event: Event)` handlers, since `$any()` is itself an `any` escape hatch
- [X] T042 Accessibility verification pass across both components: labelled inputs, `aria-invalid`/`aria-describedby` wiring, keyboard operability of the status toggle and all buttons, sufficient contrast on error/success states (Constitution Principle II, WCAG 2.1 AA) — added `role="alert"` to the dynamic error/validation banners; `angular-eslint` template accessibility rules pass
- [X] T043 Execute the manual smoke check in `specs/002-cadastro-despesa-recorrente/quickstart.md` end-to-end against `npm start` — verified via browser automation: live preview updates, blur validation, and the failure/retry path (no backend yet) all behave as specified; data is retained across the failed submit and retry
- [X] T044 Confirm frontend unit test coverage on critical paths (validation, submit, error handling) meets the project's minimum line-coverage threshold (Constitution Principle II) — added `@vitest/coverage-v8` dev dependency (plan.md/research.md §9, human-approved); `ng test --coverage` reports 100% line coverage, 98.32% statements, 93.33% branches across `features/despesa-recorrente/` with 21 passing tests

---

## Phase 8: Design Fidelity Reconciliation (Cross-Cutting)

**Purpose**: Phases 1–7 above satisfy the spec as it existed at first implementation, but plan.md's design-fidelity pass (spec Clarifications #6–#9, FR-018, FR-019, SC-006) postdates that implementation and is not yet reflected in the code: `cadastro-despesa-recorrente.component.html` still uses generic Tailwind classes (`bg-blue-600`, `border-red-400`, `text-slate-*`) instead of design tokens, has no `maxlength="100"` on `nome`, uses no Sora/Public Sans typography, and has no responsive breakpoints for narrow viewports (confirmed by direct inspection of the current template and `frontend/src/styles.css`, which is still just `@import "tailwindcss";`). This phase reconciles the implementation with `design/Cadastro.dc.html` per research.md §10–§13. It revisits markup already built in Phases 3–6 rather than adding new signals or component logic, so it is organized as one cross-cutting phase rather than per-user-story.

**Independent Test**: Load the screen at a wide viewport and visually compare colors, typography (Sora headings/amount, Public Sans body), spacing, and border-radius against `design/Cadastro.dc.html`; then resize to 360px width and confirm no horizontal scroll and every control stays reachable and legible (FR-018, FR-019, SC-006).

### Tests for Phase 8

> **Write this test FIRST, ensure it FAILS before implementation.**

- [X] T045 [P] [US3] Component test in `cadastro-despesa-recorrente.component.spec.ts`: the `nome` `<input>` carries a native `maxlength="100"` attribute (research.md §10, spec Clarification #5, FR-002) — makes T047 below pass

### Implementation for Phase 8

- [X] T046 [P] Add shared theme tokens to `frontend/src/styles.css`: the Google Fonts `@import` for Sora/Public Sans, and a Tailwind v4 `@theme` block defining `--color-accent: #2E6FF2`, `--color-accent-hover: #1B4FCB`, `--color-danger: #D92D20`, `--color-success: #0F7B4E`, `--color-border: #D0D5DD`, `--color-border-light: #E4E7EC`, `--color-muted: #667085`, `--font-heading: "Sora", sans-serif`, `--font-sans: "Public Sans", system-ui, sans-serif` (research.md §11) — makes `bg-accent`/`text-accent`/`border-accent`/`font-heading`/`font-sans` (and the danger/success/border/muted equivalents) available as ordinary utility classes app-wide — the Google Fonts `@import` was placed *before* `@import "tailwindcss";` (not after, as in research.md's snippet), since Tailwind's own `@import` expands to non-import CSS and a later `@import` after it is invalid per the CSS spec (confirmed by an Angular build warning: "All `@import` rules must come first")
- [X] T047 [US3] Add `maxlength="100"` to the `nome` `<input>` in `cadastro-despesa-recorrente.component.html` (research.md §10, spec Clarification #5, FR-002) — makes T045 pass
- [X] T048 [US1] Replace the generic Tailwind classes (`bg-blue-600`/`hover:bg-blue-700`, `border-red-400`, `text-slate-*`, `border-slate-*`, `bg-slate-*`) throughout `cadastro-despesa-recorrente.component.html` with the theme tokens from T046: the `<h1>` uses `font-heading`; body text/labels use the app-wide `font-sans` default; the submit button, active status-toggle segment, and focus rings use `bg-accent`/`border-accent`/`text-accent`; invalid-field borders/messages use `border-danger`/`text-danger`; neutral borders/muted text use `border-border`/`text-muted` — reproducing `design/Cadastro.dc.html`'s exact colors and typography through Tailwind utilities, never by copying its inline styles (FR-018, Clarification #6)
- [X] T049 [US1] Rebuild the success confirmation and error banners in `cadastro-despesa-recorrente.component.html` to match `design/Cadastro.dc.html` lines 44–68 (status icon, colored background/border box, bold heading + muted subtext, ghost action button) using the theme tokens from T046, replacing the current single-line banners (FR-018)
- [X] T050 [US2] Rebuild `despesa-preview.component.html` to match `design/Cadastro.dc.html` lines 179–197: category dot + name row, category label / due-day row, `font-heading` for the formatted value beside a "Pendente" status badge, and status helper text below — using theme tokens/Tailwind utilities instead of the current generic `slate-*` styling (FR-018)
- [X] T051 [US1] Apply responsive layout classes in `cadastro-despesa-recorrente.component.html`: wrap the form and `<app-despesa-preview>` in a container using `flex flex-col gap-6 lg:flex-row lg:items-start lg:gap-8`, and lay out the Categoria/Valor and Dia/Data-de-início field pairs with `grid grid-cols-1 sm:grid-cols-2 gap-3.5`, per research.md §12 (FR-019, SC-006)
- [X] T052 Confirm the "ContasEmDia" logo/header block shown in `design/Cadastro.dc.html` (lines 31–37) stays excluded from `cadastro-despesa-recorrente.component.html` — verification checkpoint, no code change expected (research.md §13, spec Clarification #7) — confirmed by inspection: the template's component tree still starts at the "Nova despesa recorrente" `<h1>`
- [X] T053 [P] Re-run ESLint and `cd frontend && npm run build` after T046–T051 to confirm no strict-mode/lint regressions (Constitution Principle III) — both clean, zero warnings
- [ ] T054 Re-run the manual smoke check in `specs/002-cadastro-despesa-recorrente/quickstart.md` end-to-end at a wide viewport and again at a 360px-wide viewport, confirming no horizontal scroll, full keyboard/legibility of all controls, and visual fidelity (colors, Sora/Public Sans typography, spacing, border-radius) against `design/Cadastro.dc.html` (FR-018, FR-019, SC-006) — **NOT executed this pass**: no Chrome browser instance was connected to this session (`list_connected_browsers` returned empty), so the visual/responsive check could not be driven via browser automation; needs a human (or a session with a connected browser) to run against `npm start`
- [X] T055 Re-run `ng test --coverage` after T046–T051 to confirm line coverage on `features/despesa-recorrente/` stays at or above the Phase 7 baseline (Constitution Principle II) — 100% line coverage maintained (98.42% statements, 93.43% branches, up slightly from the Phase 7 baseline)

**Checkpoint**: The implemented screen matches `design/Cadastro.dc.html`'s visual fidelity requirements (FR-018) and stays usable from 360px viewport width up (FR-019, SC-006), with all four user stories still independently functional.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **User Stories (Phase 3–6)**: All depend on Foundational phase completion
  - US1 (P1) has no dependency on other stories
  - US2 (P2) and US3 (P2) both build on the component skeleton US1 creates (T013), but each is independently testable once US1 exists
  - US4 (P3) builds on the `onSalvar()`/error-handling scaffold US1 creates (T014), and is independently testable once US1 exists
- **Polish (Phase 7)**: Depends on all four user stories being complete
- **Design Fidelity Reconciliation (Phase 8)**: Depends on Phases 3–7 being complete — it revisits the markup those phases already built; does not block or get blocked by anything downstream (this is the last phase)

### User Story Dependencies

- **US1**: Requires Foundational (T006–T009) only
- **US2**: Requires US1's component skeleton (T013) to exist before adding preview signals/wiring; independently testable once added
- **US3**: Requires US1's component skeleton (T013) and `onSalvar()` (T014) to exist before adding validation gating; independently testable once added
- **US4**: Requires US1's `onSalvar()` (T014) to exist before extending its error handling; independently testable once added
- **Phase 8**: Requires the templates built by US1/US2 (T016, T021–T023) and the validation wiring from US3 (T033) to exist, since it restyles/reworks them rather than creating new components

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Model/service (Foundational) before any component work
- Component skeleton before signals/computed logic before template wiring
- Story complete before moving to the next priority

### Parallel Opportunities

- T003 and T004 (Setup) can run in parallel
- T006 and T008 (Foundational) can run in parallel; T009 depends on both
- All three US1 test tasks (T010–T012) can run in parallel; all target the same spec file, so write them as one commit rather than truly concurrent edits
- All US2 test tasks (T018–T019) can run in parallel across their two spec files
- T021 and T022 (US2 implementation) can run in parallel — different files
- All five US3 test tasks (T024–T028) can run in parallel (same file, batch together)
- All three US4 test tasks (T034–T036) can run in parallel (same file, batch together)
- T040 and T041 (Polish) can run in parallel
- T045 and T046 (Phase 8) can run in parallel — different files (test spec vs. `styles.css`); T047–T052 then depend on T046's tokens existing and mostly touch the same two template files, so treat them as a sequential batch rather than truly concurrent edits

---

## Parallel Example: User Story 1

```bash
# Launch all US1 tests together (same spec file — write in one pass):
Task: "Component test: valid submit sends expected POST, 201 shows success banner with name"
Task: "Component test: 'Cadastrar outra despesa' resets all signals to idle"
Task: "Component test: untouched frequency/status default to Monthly/Active in payload"
```

## Parallel Example: Foundational

```bash
# T006 and T008 have no dependency on each other:
Task: "Create despesa-recorrente.model.ts with all types/DTOs"
Task: "Write failing HttpTestingController test for DespesaRecorrenteService.create()"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Fill and submit the form against `HttpTestingController`; confirm success banner shows the name
5. Demo the MVP: registration works end-to-end, without preview polish, validation gating, or failure recovery

### Incremental Delivery

1. Setup + Foundational → foundation ready
2. Add US1 → test independently → MVP demo
3. Add US2 → test independently → live preview demo
4. Add US3 → test independently → validation-blocking demo
5. Add US4 → test independently → failure-recovery demo
6. Polish (Phase 7) → final constitution-compliance pass
7. Design Fidelity Reconciliation (Phase 8) → reconcile the already-built screen with `design/Cadastro.dc.html`'s exact colors/typography/spacing and 360px-and-up responsiveness (FR-018, FR-019, SC-006)

### Parallel Team Strategy

With multiple developers, once Foundational (T006–T009) is done:
- Developer A: US1 (T010–T017) — must land first, since US2/US3/US4 build on its component skeleton
- Once US1's skeleton (T013) and `onSalvar()` (T014) exist:
  - Developer B: US2 (T018–T023)
  - Developer C: US3 (T024–T033)
  - Developer D: US4 (T034–T039)

---

## Notes

- [P] tasks = different files (or, within a single spec file, independent test cases meant to be authored together), no dependencies
- [Story] label maps task to specific user story for traceability
- This is a single-screen feature (`--routing=false`) — there is no listing/edit/pause/delete scope (spec Assumptions)
- Verify tests fail before implementing (Constitution Principle II, Red-Green-Refactor)
- Commit after each task or logical group
- Stop at any checkpoint to validate a story independently
