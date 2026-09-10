---

description: "Task list for Editar Despesa Recorrente (Tela)"
---

# Tasks: Editar Despesa Recorrente (Tela)

**Input**: Design documents from `/specs/008-edit-recurring-expense-ui/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/edit-screen-contract.md, quickstart.md

**Tests**: Required by this project's constitution (Principle II — Test-First Development). Vitest component/service tests are written before the implementation task they verify, following Red-Green-Refactor, with one test per distinct scenario each layer can produce — same pattern already used by `cadastro-despesa-recorrente.component.spec.ts` and `despesa-recorrente.service.spec.ts`.

**Organization**: Tasks are grouped by user story (spec.md priorities P1–P4). Tasks that append scenarios to an already-created spec file (`despesa-recorrente.service.spec.ts`, `editar-despesa-recorrente.component.spec.ts`, `painel-mensal-despesas.component.spec.ts`) are listed sequentially (not `[P]`) across stories to avoid edit conflicts, even when logically independent — same convention already used by `specs/007-edit-recurring-expense/tasks.md`.

**Design fidelity**: Every markup/copy task below MUST reproduce `design/Editar.dc.html` (and the edit icon added to `design/Main.dc.html`) exactly — same field order, button labels, state copy, icon, and Tailwind-equivalent visual tokens (colors, radii, spacing) already fixed by that prototype — translating its inline HTML/CSS to the Angular template + Tailwind utility classes already used by `CadastroDespesaRecorrenteComponent`, per Princípio VIII.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Paths are relative to the repository root (`frontend/...`, `backend/...`, `design/...`)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization.

No setup tasks are required. This feature adds no new project — it extends the existing Angular app (`frontend/`) already scaffolded by `002-cadastro-despesa-recorrente`/`006-painel-mensal-despesas`, plus one small cross-feature addition to the already-existing `GetMonthlyPanelUseCase` (backend).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared types and the extracted form utility that both the new edit screen and the existing cadastro screen depend on. **No User Story implementation can begin until this phase is complete.**

- [X] T001 [P] Add `RecurringExpenseDetailResponse`, `UpdateRecurringExpenseRequest`, and `UpdateRecurringExpenseResponse` interfaces to `frontend/src/app/features/despesa-recorrente/despesa-recorrente.model.ts`, per `contracts/edit-screen-contract.md` (reuse existing `CategoryValue`/`FieldError`/`ApiErrorResponse`, no duplication) — also added `ApiEnvelope<T>` here (the real API response shape) and had `painel-mensal-despesas.model.ts` import/re-export it instead of duplicating it, per the existing re-export pattern in that file
- [X] T002 [P] Extract the pure field-parsing/validation functions (`parseValor`, `parseDia`, and the `validate()` rule set for nome/valor/dia/dataInicio) currently private in `frontend/src/app/features/despesa-recorrente/cadastro-despesa-recorrente/cadastro-despesa-recorrente.component.ts` into a new shared file `frontend/src/app/shared/recurring-expense-form.util.ts`, preserving exact behavior (research.md, Decisão 2)
- [X] T003 Update `cadastro-despesa-recorrente.component.ts` to import and use the functions from `recurring-expense-form.util.ts` (T002) instead of its own private copies; no behavior change — `cadastro-despesa-recorrente.component.spec.ts` must keep passing unchanged (depends on T002)
- [X] T004 Add `getById(id: string)` and `update(id: string, payload: UpdateRecurringExpenseRequest)` methods to `frontend/src/app/features/despesa-recorrente/despesa-recorrente.service.ts`, calling `GET`/`PUT /api/v1/recurring-expenses/{id}` via the already-injected `HttpClient`, alongside the existing `create()` method (depends on T001) — returns `Observable<ApiEnvelope<T>>` (unwrapped by the consuming component), matching the real backend envelope and the convention already used by `PainelMensalDespesasService`, not the older unenveloped pattern in `create()`

**Checkpoint**: `frontend`: `npm test` passes (existing cadastro tests still green) and the app still builds. User story implementation can now begin.

---

## Phase 3: User Story 1 - Editar os dados de uma despesa recorrente numa tela dedicada (Priority: P1) 🎯 MVP

**Goal**: Opening the edit screen for an existing recurring expense pre-fills it with current values; changing valid fields and saving shows a success confirmation with the updated data; client-side invalid submissions are blocked with inline errors; saving without changing anything still succeeds.

**Independent Test**: Navigate directly to `despesas/{id}/editar` for an existing recurring expense, verify the form pre-fills, change a valid field, save, and verify the success confirmation shows the updated value (no "cadastrar outra despesa" action present).

### Tests for User Story 1

> Write these tests first; they must fail (methods/component do not exist yet) before the implementation tasks below.

- [X] T005 [P] [US1] Write failing tests in `frontend/src/app/features/despesa-recorrente/despesa-recorrente.service.spec.ts`: `getById(id)` GETs `/api/v1/recurring-expenses/{id}` and resolves with `RecurringExpenseDetailResponse` on `200`; `update(id, payload)` PUTs the same URL with `payload` as body and resolves with `UpdateRecurringExpenseResponse` on `200` (mirrors the existing `create()` test pattern) — implemented together with T019's service-side error-path tests (404/network/400) in the same file edit, since both target the same file
- [X] T006 [P] [US1] Create `frontend/src/app/features/despesa-recorrente/editar-despesa-recorrente/editar-despesa-recorrente.component.spec.ts` with failing tests: on init, calls `getById` with the route's `id` and pre-fills every editable field once the response resolves (FR-003); editing a field updates the preview in real time; saving valid changes transitions `formStatus` to `'loading'` then `'success'` and renders a confirmation containing the updated name, with no "cadastrar outra despesa" affordance present (FR-010); submitting with an invalid field is blocked and reveals every invalid field's error at once (FR-007/FR-008); the save button shows a distinct "Salvando…" state while `formStatus === 'loading'` (FR-009); saving with zero changed fields still resolves to `'success'` (FR-015); the status helper text under the "Ativa" control is visible persistently (not just at the instant of toggling) and mentions that an occurrence may be generated on save, updating immediately when toggled from "Pausada" (FR-016) — written together with T016's (US3) and T020's (US4) tests in the same file, since a single realistic pass through the component's states covered all three story's scenarios at once

### Implementation for User Story 1

- [X] T007 [US1] Create `frontend/src/app/features/despesa-recorrente/editar-despesa-recorrente/editar-despesa-recorrente.component.ts`: standalone component reading `id` from the activated route, signals/computeds per `data-model.md` (`nome`, `categoria`, `valor`, `dia`, `dataInicio`, `status`, `observacao`, `loadStatus`, `formStatus`, `touched`, `submitAttempted`, `apiFieldErrors`), using `recurring-expense-form.util.ts` (T002) for parsing/validation, `DespesaPreviewComponent` unmodified for the preview column, and `DespesaRecorrenteService.getById`/`update` (T004) for data access; includes the persistent `statusHelperLabel` computed for FR-016 (depends on T001, T002, T004, T005, T006) — implemented together with T017 (US3) and T021/T022 (US4), since they're all part of the same component class
- [X] T008 [US1] Create `frontend/src/app/features/despesa-recorrente/editar-despesa-recorrente/editar-despesa-recorrente.component.html`: template for the `loaded` state (idle/saving/success), reproducing `design/Editar.dc.html` field-for-field — header, "Voltar ao painel" link, title/subtitle, persistent info note about occurrences not being rewritten, form fields in the same order (Nome, Categoria, Valor previsto mensal, Dia de vencimento, Data de início, Frequência fixa "Mensal", Status, Observação), "Salvar alterações"/"Cancelar" buttons, success banner (no "cadastrar outra despesa" button), and the preview column — using Tailwind utility classes equivalent to the design's inline styles (depends on T007) — implemented together with T018 (US3 dialog) and T021 (US4 not-found/load-error/skeleton) markup, all in the same template file
- [X] T009 [US1] Add route `{ path: 'despesas/:id/editar', component: EditarDespesaRecorrenteComponent }` to `frontend/src/app/app.routes.ts`, alongside the existing `despesas/nova` route (depends on T007)

**Checkpoint**: `EditarDespesaRecorrenteComponent` is fully functional and independently testable by navigating directly to its route. (Not yet reachable from the monthly panel — that's US2.)

---

## Phase 4: User Story 2 - Acessar a edição a partir do painel mensal (Priority: P2)

**Goal**: An edit icon on each monthly-panel row opens the edit screen for the recurring expense that owns that row's occurrence (not the occurrence itself).

**Independent Test**: Click the edit icon on a monthly-panel row and verify the edit screen opens with the data of the recurring expense that owns that occurrence.

### Prerequisite fix (cross-feature, blocks this story — see plan.md "Cross-Feature Dependency")

- [X] T010 [US2] In `backend/Application/UseCases/GetMonthlyPanel/GetMonthlyPanelUseCase.cs`, add `RecurringExpenseId` to the `PanelOccurrenceData` record and pass `expense.GetId()` into each projected item (the `.SelectMany(expense => expense.GetOccurrencesForPeriod(...))` projection currently discards it — research.md, Decisão 5) — added a xUnit test first (`GetMonthlyPanelUseCaseTests.ExecuteAsync_Occurrence_CarriesOwningRecurringExpenseId`); also had to fix two other constructors of the same shared `PanelOccurrenceData` record in `MarkOccurrenceAsPaidUseCase`/`UndoOccurrencePaymentUseCase` (not anticipated in the original task text), since adding a positional field broke them too — full `dotnet test` suite (182 tests) passes
- [X] T011 [US2] Add `RecurringExpenseId` to `PanelOccurrenceDataResponse` in `backend/Api/Responses/PanelOccurrenceDataResponse.cs` and to its mapping, so the field reaches the JSON response as `recurringExpenseId` (depends on T010)

### Tests for User Story 2

- [X] T012 [US2] Add a failing test to `frontend/src/app/features/painel-mensal-despesas/painel-mensal-despesas.component.spec.ts`: each row's edit icon exposes a `routerLink` (or navigation call) targeting `/despesas/{recurringExpenseId}/editar` — using the occurrence's owning recurring expense id, not the occurrence's own id

### Implementation for User Story 2

- [X] T013 [P] [US2] Add `recurringExpenseId: string` to `PanelOccurrenceResponse` in `frontend/src/app/features/painel-mensal-despesas/painel-mensal-despesas.model.ts` (depends on T011) — also had to add the field to two inline `PanelOccurrenceResponse`/occurrence literals in `painel-mensal-despesas.service.spec.ts` that don't go through the `makeOccurrence()` test helper
- [X] T014 [US2] Update the item-mapping logic in `frontend/src/app/features/painel-mensal-despesas/painel-mensal-despesas.component.ts` to carry `recurringExpenseId` from `PanelOccurrenceResponse` through to each rendered display item (depends on T013)
- [X] T015 [US2] Add the edit icon button to `frontend/src/app/features/painel-mensal-despesas/painel-mensal-despesas.component.html`, reproducing the pencil icon already added to `design/Main.dc.html` exactly (SVG path, size, border, `aria-label`/`title` "Editar despesa recorrente"), placed beside the existing per-row actions, with `routerLink="/despesas/{{ item.recurringExpenseId }}/editar"` (depends on T012, T014) — used the `[routerLink]="['/despesas', item.recurringExpenseId, 'editar']"` array form instead of a single interpolated string (equivalent, avoids manual string concatenation)

**Checkpoint**: Clicking the edit icon on any monthly-panel row opens the correct recurring expense's edit screen.

---

## Phase 5: User Story 3 - Ser avisado antes de perder alterações não salvas (Priority: P3)

**Goal**: Leaving the edit screen with at least one field whose value differs from what was loaded prompts a confirmation; leaving without any such difference (including after manually reverting a change) does not.

**Independent Test**: Change a field and attempt to leave — confirm the dialog appears; revert that field to its original value and attempt to leave again — confirm no dialog appears.

### Tests for User Story 3

- [X] T016 [US3] Add failing tests to `editar-despesa-recorrente.component.spec.ts`: changing any field and invoking "Cancelar" shows the exit-confirmation dialog (FR-013); reverting that field to its originally-loaded value before invoking "Cancelar" shows no dialog, i.e. the comparison is by value against the loaded snapshot, not by "was ever touched" (FR-014, research.md Decisão 4) — written together with T006 (see note there)

### Implementation for User Story 3

- [X] T017 [US3] Implement `initialSnapshot` signal (captured on successful load, updated after a successful save) and `hasUnsavedData` computed (value comparison of all seven editable fields against `initialSnapshot`) plus `onClickVoltar`/`onCancelExit`/`onConfirmExit` handlers in `editar-despesa-recorrente.component.ts` (depends on T007, T016) — implemented together with T007 (see note there)
- [X] T018 [US3] Add the exit-confirmation dialog markup to `editar-despesa-recorrente.component.html`, reproducing `design/Editar.dc.html`'s dialog exactly ("Sair sem salvar?", body copy, "Continuar editando"/"Sair sem salvar" buttons) (depends on T008, T017) — implemented together with T008 (see note there)

**Checkpoint**: Unsaved-changes protection works exactly as clarified (value-based, no false positives on revert).

---

## Phase 6: User Story 4 - Ser informado com clareza quando algo impede a edição (Priority: P4)

**Goal**: A missing recurring expense, a failed load, or a failed save each show a dedicated, actionable state instead of a dead end or a silent failure.

**Independent Test**: Simulate each of the three failure conditions (404 on load, network failure on load, network/validation failure on save) and verify each renders its own message with a way to continue (retry or go back), and that a failed save never discards what was typed.

### Tests for User Story 4

- [X] T019 [US4] Add failing tests to `despesa-recorrente.service.spec.ts`: `getById` surfaces a `404` and a network failure as-is to the caller; `update` surfaces a `400` (with field errors), a `404`, and a network failure as-is to the caller (mirrors the existing `create()` error-path tests) — done ahead of schedule, alongside T005 (same file)
- [X] T020 [US4] Add failing tests to `editar-despesa-recorrente.component.spec.ts`: a `404` from `getById` sets `loadStatus` to `'not-found'` and renders the not-found message without any form field (FR-005); a network/5xx failure from `getById` sets `loadStatus` to `'error'` and "Tentar novamente" re-issues the same request (FR-006); a failed `update` sets `formStatus` to `'error'`, keeps every field's current (edited) value visible and editable (FR-011), and "Tentar novamente" re-issues the save; a `400` response with field errors maps each error onto its corresponding field inline instead of only a generic banner (FR-012) — written together with T006 (see note there)

### Implementation for User Story 4

- [X] T021 [US4] Implement the `loadStatus` state machine (`'loading' → 'loaded' | 'not-found' | 'error'`, with a retry path back to `'loading'`) and its three non-`'loaded'` render branches in `editar-despesa-recorrente.component.ts`/`.html`, reproducing `design/Editar.dc.html`'s loading skeleton, not-found icon/copy, and load-error banner exactly (depends on T007, T008, T020) — implemented together with T007/T008 (see notes there)
- [X] T022 [US4] Implement save-failure handling in `editar-despesa-recorrente.component.ts` (`formStatus` set to `'error'` on failure, `apiFieldErrors` populated from a `400`'s field errors, generic fallback message otherwise) and its banner in `editar-despesa-recorrente.component.html`, reproducing `design/Editar.dc.html`'s save-error banner copy exactly ("Não foi possível salvar as alterações", "Tentar novamente") (depends on T007, T021) — implemented together with T007/T008 (see notes there)

**Checkpoint**: All four user stories are independently functional; every failure path has a clear next step.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Validation and cleanup that spans all stories.

- [X] T023 [P] Run every scenario in `specs/008-edit-recurring-expense-ui/quickstart.md` (1-6) manually end-to-end against the running app (`dotnet run` + `npm start`) — ran against the real backend/Postgres via Chrome: pre-fill with real data confirmed (including a case where the occurrence's snapshotted name/value visibly differed from the despesa's current data, proving FR-003), the persistent reactivation helper text (FR-016), a real Pausada→Ativa save that visibly created a new occurrence in the monthly panel (3→4 contas, confirming the backend side-effect end-to-end), and the not-found state (FR-005) all verified visually; added a throwaway `frontend/proxy.conf.json` (not wired into `angular.json`, opt-in via `--proxy-config`) since no dev-time API proxy existed yet — no console errors observed
- [X] T024 [P] Verify WCAG 2.1 AA basics on the new screen and the new painel-mensal icon (labels/`aria-label` on every input and the edit icon button, logical focus order, sufficient color contrast) per Princípio II — every input has a `<label for>`, error text is wired via `aria-describedby`/`aria-invalid`, the exit dialog uses `role="alertdialog"`/`aria-modal`, added `role="status"`/`aria-live="polite"` to the loading skeleton, the edit icon has `aria-label`/`title`; colors reuse the already-approved cadastro/painel palette, no new contrast risk introduced
- [X] T025 Remove any now-unused private validation/parsing code left behind in `cadastro-despesa-recorrente.component.ts` after the extraction in T002/T003 — verified no leftover `parseValor`/`parseDia`/validation duplicates remain (checked via grep); nothing to remove

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: None — skipped, no tasks.
- **Foundational (Phase 2)**: No dependencies — BLOCKS all user stories (T001-T004).
- **User Story 1 (Phase 3)**: Depends on Foundational. Delivers the MVP (edit screen reachable by direct URL).
- **User Story 2 (Phase 4)**: Depends on Foundational; independent of US1's internals but needs `EditarDespesaRecorrenteComponent`'s route (T009) to link to, and its own backend prerequisite (T010-T011).
- **User Story 3 (Phase 5)**: Depends on Foundational and on US1's component existing (T007/T008) to extend.
- **User Story 4 (Phase 6)**: Depends on Foundational and on US1's component existing (T007/T008) to extend.
- **Polish (Phase 7)**: Depends on all four user stories being complete.

### User Story Dependencies

- **US1 (P1)**: No dependency on other stories — the MVP.
- **US2 (P2)**: Structurally independent (own backend fix + own component file), but only delivers user-reachable value once US1's route exists.
- **US3 (P3)**: Extends US1's component file (`editar-despesa-recorrente.component.ts`/`.html`) — not parallelizable with US1 on the same files, but independently testable once US1 is done.
- **US4 (P4)**: Extends US1's component file — same note as US3; US3 and US4 touch the same two files (component + its spec) so should be done sequentially by whoever implements them, even though they are logically independent.

### Parallel Opportunities

- T001 and T002 (Foundational) can run in parallel — different files, no shared dependency.
- T005 and T006 (US1 tests) can run in parallel — different files (`despesa-recorrente.service.spec.ts` vs. a new `editar-despesa-recorrente.component.spec.ts`).
- T013 (US2, frontend model) can run in parallel with T012 (US2, existing painel spec file) once T011 lands.
- T023 and T024 (Polish) can run in parallel.
- US2's backend prerequisite (T010-T011) can be implemented by a different person/session than US1's frontend work, since they touch entirely disjoint files.

---

## Parallel Example: Foundational Phase

```bash
Task: "Add RecurringExpenseDetailResponse/UpdateRecurringExpenseRequest/UpdateRecurringExpenseResponse to despesa-recorrente.model.ts"
Task: "Extract parseValor/parseDia/validate() into shared/recurring-expense-form.util.ts"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 2: Foundational.
2. Complete Phase 3: User Story 1.
3. **STOP and VALIDATE**: open `despesas/{id}/editar` directly for a known recurring expense id and run quickstart scenarios 1-2.
4. Demo if ready — the screen works, just not yet linked from the monthly panel.

### Incremental Delivery

1. Foundational → ready.
2. US1 → edit screen works standalone (MVP).
3. US2 → edit screen becomes reachable from the monthly panel (needs the backend id fix, T010-T011).
4. US3 → unsaved-changes protection added.
5. US4 → not-found/load-error/save-error states added.
6. Each story adds value without breaking the previous ones — by the time US4 lands, all of `quickstart.md` should pass.

---

## Notes

- `[P]` tasks = different files, no dependencies.
- `[Story]` label maps task to specific user story for traceability.
- US3 and US4 both extend the same two files US1 creates (`editar-despesa-recorrente.component.ts`/`.html` and their spec) — implement them sequentially, not concurrently, to avoid merge conflicts, even though they are logically independent.
- Every visual/copy task explicitly cites `design/Editar.dc.html` or `design/Main.dc.html` as the source of truth — do not improvise wording, colors, or field order not present there.
- Verify tests fail before implementing.
- Commit after each task or logical group.
- Stop at any checkpoint to validate a story independently.
