# Data Model: Cadastro de Despesa Recorrente (Frontend)

**Feature**: `002-cadastro-despesa-recorrente` | **Date**: 2026-09-02

This is a frontend feature: "entities" here are TypeScript types/DTOs and
component state (Signals), not backend domain objects — the authoritative
domain model lives in
[`specs/001-despesa-recorrente-domain/data-model.md`](../001-despesa-recorrente-domain/data-model.md).
Everything below lives under
`frontend/src/app/features/despesa-recorrente/` (Constitution Principle VII).

## Types & DTOs — `despesa-recorrente.model.ts`

```ts
type CategoryValue = 'Housing' | 'Services' | 'Transportation' | 'Subscriptions' | 'Other';
type StatusValue = 'ativa' | 'pausada';
type FormStatus = 'idle' | 'loading' | 'success' | 'error';

interface CategoryOption { value: CategoryValue; label: string; }

const CATEGORY_OPTIONS: CategoryOption[] = [
  { value: 'Housing', label: 'Moradia' },
  { value: 'Services', label: 'Serviços' },
  { value: 'Transportation', label: 'Transporte' },
  { value: 'Subscriptions', label: 'Assinaturas' },
  { value: 'Other', label: 'Outra' },
];

const CATEGORY_COLORS: Record<CategoryValue, string> = {
  Housing: '#2E6FF2',
  Services: '#0E9384',
  Transportation: '#B8790A',
  Subscriptions: '#7A5AF8',
  Other: '#667085',
};

interface CreateRecurringExpenseRequest {
  name: string;
  category: CategoryValue;
  monthlyAmount: number;
  dueDay: number;
  startDate: string;   // ISO 8601 yyyy-MM-dd
  frequency: 'Monthly';
  status: 'Active' | 'Paused';
  note: string | null;
}

interface OccurrenceResponse {
  id: string;
  referencePeriod: { year: number; month: number };
  dueDate: string;
  status: 'Pending';
  expectedAmount: number;
  name: string;
  category: CategoryValue;
}

interface CreateRecurringExpenseResponse {
  id: string;
  name: string;
  category: CategoryValue;
  monthlyAmount: number;
  dueDay: number;
  startDate: string;
  frequency: 'Monthly';
  status: 'Active' | 'Paused';
  note: string | null;
  occurrences: OccurrenceResponse[];
}

interface FieldError { field: string; message: string; }
interface ApiErrorResponse { errors: FieldError[]; }
```

`CATEGORY_COLORS` is keyed by the same `CategoryValue` sent to the API
(not by the PT-BR label), so `catColor` (below) needs no secondary lookup
through `CATEGORY_OPTIONS`. These five hexes already match
`design/Cadastro.dc.html`'s `CATEGORY_COLORS` map exactly (FR-018) — no
change needed here. The design's `accent` value (`#2E6FF2`, used for the
primary button, focus rings, and the "Ativa" status segment) is **not**
duplicated in this model file — per spec Clarification #9 it comes from the
shared `--color-accent` Tailwind theme token defined once in
`frontend/src/styles.css` (see [research.md](research.md) §11), consumed in
templates as `bg-accent`/`text-accent`/`border-accent` utility classes, not
as a TypeScript constant here or as Tailwind's default `blue-600`.

## Component state — `CadastroDespesaRecorrenteComponent` (signals)

| Signal | Type | Initial value | Notes |
|---|---|---|---|
| `nome` | `signal<string>` | `''` | FR-002; bound to `<input id="nome" maxlength="100">` in the template so the browser itself blocks typing or pasting past 100 characters (spec Clarification #5) — `nomeError`'s length check stays as a defense-in-depth/backstop, not the primary enforcement |
| `categoria` | `signal<CategoryValue>` | `'Housing'` | FR-003; first `CATEGORY_OPTIONS` entry |
| `valor` | `signal<string>` | `''` | raw typed text, parsed on demand (FR-004) |
| `dia` | `signal<string>` | `''` | raw typed text, parsed on demand (FR-005) |
| `dataInicio` | `signal<string>` | `''` | raw typed text `dd/mm/aaaa` (FR-006) |
| `status` | `signal<StatusValue>` | `'ativa'` | FR-008 |
| `observacao` | `signal<string>` | `''` | FR-009, no validation |
| `touched` | `signal<{ nome: boolean; valor: boolean; dia: boolean; dataInicio: boolean }>` | all `false` | set on `(blur)` (FR-012) |
| `submitAttempted` | `signal<boolean>` | `false` | set on invalid submit attempt (FR-011) |
| `formStatus` | `signal<FormStatus>` | `'idle'` | drives loading/success/error UI (FR-013–FR-017) |
| `submitErrorMessage` | `signal<string \| null>` | `null` | generic banner text when `formStatus === 'error'` and no field-level match (FR-016) |
| `savedName` | `signal<string \| null>` | `null` | name to show in the success banner (FR-014); captured at submit time, independent of later edits |

`onNovaDespesa` (FR-014) resets every signal above to its initial value.

## Derived state — `computed()`

| Computed | Type | Rule | Requirement |
|---|---|---|---|
| `nomePreview` | `string` | `nome().trim() \|\| 'Nome da despesa'` | FR-010, US2-1 |
| `categoriaLabel` | `string` | `CATEGORY_OPTIONS.find(o => o.value === categoria())!.label` | US2-2 |
| `catColor` | `string` | `CATEGORY_COLORS[categoria()] ?? '#667085'` | US2-2 |
| `valorNum` | `number \| null` | parse `valor()` (accepts `1234,56` / `1234.56`); `null` if not parseable | feeds `valorFmt` + validation |
| `valorFmt` | `string` | `Intl.NumberFormat('pt-BR', {style:'currency',currency:'BRL'}).format(valorNum() ?? 0)` | FR-010, US2-3 |
| `diaLabel` | `string` | valid integer 1–31 → `` `Dia ${dia()}` ``; else `'Dia --'` | FR-010, US2-4 |
| `statusHelperLabel` | `string` | `status() === 'ativa'` → "Ativa — a ocorrência deste mês será gerada automaticamente."; else → "Pausada — nenhuma ocorrência será gerada até reativar." | US2-5 |
| `isAtiva` / `isPausada` | `boolean` | derived from `status()` | toggle rendering |
| `isLoading` / `isSuccess` / `isError` | `boolean` | derived from `formStatus()` | US1/US4 visual states |
| `nomeError` | `string \| null` | `''` after trim → "Nome é obrigatório."; length > 100 → "Nome deve ter no máximo 100 caracteres."; else `null` | FR-002, US3-1/2 |
| `valorError` | `string \| null` | not parseable, `<= 0`, or more than 2 decimals → message; else `null` | FR-004, US3-3 |
| `diaError` | `string \| null` | not an integer in `1..31` → message; else `null` | FR-005, US3-4 |
| `dataInicioError` | `string \| null` | not a real calendar date in `dd/mm/aaaa` → message; else `null` | FR-006, US3-5 |
| `isFormValid` | `boolean` | `!nomeError() && !valorError() && !diaError() && !dataInicioError()` | FR-011 |
| `showNomeError` / `showValorError` / `showDiaError` / `showDataInicioError` | `boolean` | `(touched().<campo> \|\| submitAttempted()) && <campo>Error() !== null` | FR-012, mirrors prototype's `reveal()` |

Validation itself (the `*Error` computeds) always runs on every keystroke;
only the corresponding `show*Error` computed decides whether the message is
currently revealed — this is what makes FR-012 ("as soon as the user leaves
the field, or at the latest when they try to save") and SC-003 (synchronous,
no debounce) hold simultaneously.

## Presentational component — `DespesaPreviewComponent`

Receives everything through `input()`, computes nothing of its own domain
logic beyond pure display (Constitution Principle IX — inputs are read-only):

| Input | Type | Source |
|---|---|---|
| `nome` | `input<string>()` | `nomePreview()` |
| `categoriaLabel` | `input<string>()` | `categoriaLabel()` |
| `catColor` | `input<string>()` | `catColor()` |
| `valorFmt` | `input<string>()` | `valorFmt()` |
| `diaLabel` | `input<string>()` | `diaLabel()` |
| `statusHelperLabel` | `input<string>()` | `statusHelperLabel()` |

## Service — `DespesaRecorrenteService`

| Method | Signature | Behavior |
|---|---|---|
| `create` | `create(payload: CreateRecurringExpenseRequest): Observable<CreateRecurringExpenseResponse>` | `HttpClient` (injected via `inject()`) `POST` to `/api/recurring-expenses`. No client-side retry/timeout logic (per spec Clarification #3) — errors are surfaced as-is to the caller. |

The component subscribes once per submit, guarded by `formStatus() ===
'loading'` (FR-013) to prevent duplicate in-flight submissions — mirrors the
domain's `onSalvar` guard already documented in the refinement.

## State machine — `formStatus`

```
idle --(onSalvar, form valid)--> loading
loading --(201 Created)--> success
loading --(400 / network / 5xx)--> error
error --(onSalvar / "Tentar novamente")--> loading
success --(onNovaDespesa)--> idle
```

`idle` is also where the form starts and where it returns after
`onNovaDespesa`. There is no transition out of `loading` other than a
server response (no client timeout — spec Clarification #3).

## Validation summary (client-side, mirrors backend FR-002–FR-008)

| Field | Required | Rule |
|---|---|---|
| Nome | Yes | non-empty after trim; ≤ 100 characters, enforced both natively (`maxlength="100"` on the input — spec Clarification #5) and by `nomeError` as a backstop |
| Categoria | Yes | one of the 5 fixed `CATEGORY_OPTIONS` values (always satisfied — closed `<select>`) |
| Valor previsto mensal | Yes | parseable, `> 0`, ≤ 2 decimal places |
| Dia de vencimento | Yes | integer, `1..31` |
| Data de início | Yes | valid calendar date |
| Frequência | Yes (fixed) | always `Monthly`, no user interaction |
| Status | Yes | `ativa` \| `pausada`, default `ativa` |
| Observação | No | free text, no validation |
