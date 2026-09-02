# Quickstart: Validating the Cadastro de Despesa Recorrente Screen

**Feature**: `002-cadastro-despesa-recorrente` | **Date**: 2026-09-02

This guide validates the Angular frontend screen in isolation. There is no
live backend endpoint yet (see [`contracts/api-contract.md`](contracts/api-contract.md)
and [`research.md`](research.md) §6) — automated tests simulate every server
response; the manual smoke check below is limited to what's actually
observable without a real API. See [`data-model.md`](data-model.md) for full
signal/computed/type details.

## Prerequisites

- Node.js and npm installed, Angular CLI v22 available (`npx @angular/cli
  version` reports `22.x`).
- Repository cloned; working directory is the repo root.

## Setup (one-time scaffolding)

```bash
npx @angular/cli new frontend --directory=frontend --style=css --routing=false --skip-git
cd frontend
npm install tailwindcss @tailwindcss/postcss postcss --save-dev
```

Then, per `angular.dev/guide/tailwind`: add a `.postcssrc.json` with the
`@tailwindcss/postcss` plugin, and replace the contents of `src/styles.css`
with `@import "tailwindcss";`.

(Exact commands/flags are illustrative; the implementation phase produces
the final `angular.json`/`package.json` under source control, per the plan's
Structure Decision. `testRunner: vitest` is expected to already be the CLI
default for `ng new` — see [`research.md`](research.md) §2.)

## Build

```bash
cd frontend
npm run build
```

Expected: builds with no TypeScript errors, strict mode enabled, no `any`
(Constitution Principle III).

## Run unit/component tests

```bash
cd frontend
npm test
```

Expected: all tests pass. At minimum, the suite MUST cover — see `spec.md`
for full scenario text:

| Scenario | Spec reference | Expected result |
|---|---|---|
| Fill all required fields validly, click "Salvar despesa" | US1-1 | `HttpTestingController` receives one `POST` with the expected `CreateRecurringExpenseRequest`; on a flushed `201`, success banner shows the submitted name. |
| Click "Cadastrar outra despesa" after success | US1-2 | All signals reset to initial values; form is empty and `formStatus() === 'idle'`. |
| Leave frequency untouched | US1-3 | Submitted payload has `frequency: 'Monthly'`. |
| Leave status untouched | US1-4 | Submitted payload has `status: 'Active'`; response with 1 occurrence renders as such if asserted. |
| Type into each field | US2-1..5 | `nomePreview`/`categoriaLabel`+`catColor`/`valorFmt`/`diaLabel`/`statusHelperLabel` update synchronously (no `fakeAsync`/`tick()` needed — proves SC-003). |
| Empty "Nome", blur or submit | US3-1 | `showNomeError()` becomes `true` with the required-field message. |
| "Nome" > 100 chars, blur or submit | US3-2 | `showNomeError()` shows the max-length message. |
| Type or paste 100+ characters into "Nome" | FR-002, spec Clarification #5 | The input's `maxlength="100"` attribute blocks any character beyond the 100th — `nome()` never holds more than 100 characters, so `nomeError()`'s length branch is unreachable in normal browser usage (research.md §10). |
| "Valor" zero/negative/> 2 decimals, blur or submit | US3-3 | `showValorError()` shows the corresponding message. |
| "Dia" outside 1–31, blur or submit | US3-4 | `showDiaError()` shows the corresponding message. |
| "Data de início" invalid, blur or submit | US3-5 | `showDataInicioError()` shows the corresponding message. |
| Submit with multiple invalid fields | US3-6 | All relevant `show*Error` become `true` at once; generic "corrija os campos" notice shown; `HttpTestingController.expectNone(...)` confirms no request was sent. |
| Flush a network/`5xx` failure on a valid submit | US4-1 | `formStatus() === 'error'`, generic banner shown, all field signals retain their values. |
| Click "Tentar novamente" after an error | US4-2 | A second identical `POST` is sent with the same payload, no re-entry required. |
| Flush a `400` with `errors: [{ field: 'name', ... }]` | US4-3 | The `name` field's inline error is populated from the response, not just the generic banner. |
| Double-click "Salvar despesa" while `formStatus() === 'loading'` | Edge Cases | `HttpTestingController` sees exactly one request, not two. |

Component tests for `DespesaPreviewComponent` should independently verify
it renders whatever is passed via its `input()`s, with no logic of its own.

## Manual smoke check

```bash
cd frontend
npm start
```

Then, in a browser, exercise the golden path and edge cases directly against
the running dev server:

- Fill the form and confirm the preview card updates on every keystroke —
  no separate "update" action, no visible delay (SC-003).
- Leave a required field empty/invalid and blur it — confirm the inline
  error appears without clicking Save (FR-012).
- Try to type or paste more than 100 characters into "Nome" — confirm the
  field simply stops accepting input at 100 characters (FR-002, spec
  Clarification #5).
- Click "Salvar despesa" with the form invalid — confirm every invalid
  field is highlighted at once plus the "corrija os campos" notice, and
  confirm no network request fires (use the browser devtools Network tab).
- Click "Salvar despesa" with the form valid — since `POST
  /api/recurring-expenses` does not exist yet, expect the request to fail
  (connection refused / 404 depending on how the dev server is configured);
  confirm this correctly renders the generic error banner with "Tentar
  novamente" (FR-015) and that all typed data is still present afterward
  (SC-004). This exercises the *failure* path end-to-end; the `201`/`400`
  success and field-mapping paths are only verifiable today via the
  automated `HttpTestingController` tests above, until a future backend
  feature implements this contract.

### Design fidelity checks (FR-018/FR-019, spec Clarifications #6–#9)

Compare the running screen side-by-side against `design/Cadastro.dc.html`
(open the `.dc.html` file directly in a browser, or via the design tool it
was authored in):

- Headings ("Nova despesa recorrente") and the preview card's formatted
  amount render in Sora; body text, labels, inputs, and buttons render in
  Public Sans — not the browser/Tailwind default sans stack.
- The primary "Salvar despesa" button, input focus rings, and the "Ativa"
  segment of the status toggle use the same accent blue as the design
  (`#2E6FF2`) — inspect the computed color in devtools and confirm it
  resolves through the `--color-accent` token in `styles.css` (research.md
  §11), not a Tailwind default (`blue-600`) or an inline style.
- The "ContasEmDia" logo/icon header at the top of the design file does
  **not** appear on this screen (research.md §13) — the page starts at the
  "Nova despesa recorrente" heading.
- Resize the browser (or use devtools device toolbar) down to 360px wide:
  the form, preview card, buttons, and every error message stay legible,
  fully reachable, and non-overlapping, with no horizontal scrollbar
  (SC-006); the two-column form/preview arrangement and the
  Categoria/Valor and Dia/Data-de-início field pairs collapse to a single
  column below their breakpoint instead of overflowing (research.md §12).
- Tab through the entire form with the keyboard only (no mouse): every
  input, the status toggle's two buttons, and "Salvar despesa" are reachable
  and operable in a logical order, with a visible focus indicator
  (Constitution Principle II, WCAG 2.1 AA).
