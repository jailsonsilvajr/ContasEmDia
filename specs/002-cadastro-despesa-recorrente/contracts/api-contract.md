# API Contract: `POST /api/recurring-expenses`

**Feature**: `002-cadastro-despesa-recorrente` | **Date**: 2026-09-02

**Status**: Documentation only — this endpoint is **not implemented** by
this feature. Only `backend/Domain` (see
[`specs/001-despesa-recorrente-domain`](../../001-despesa-recorrente-domain/))
exists on the backend today. This contract is the frontend's dependency
specification for a future backend Application/API feature, and the target
this frontend feature's `DespesaRecorrenteService` is built against (see
[`research.md`](../research.md) §6 for how this feature validates its
behavior without a live implementation).

This is the single endpoint this screen depends on. Category options are
hardcoded on the client (see [`data-model.md`](../data-model.md)
`CATEGORY_OPTIONS`) — there is no categories-listing endpoint.

## Request

`POST /api/recurring-expenses`

```json
{
  "name": "Aluguel",
  "category": "Housing",
  "monthlyAmount": 1850.00,
  "dueDay": 10,
  "startDate": "2026-09-01",
  "frequency": "Monthly",
  "status": "Active",
  "note": null
}
```

| Field | Type | Required | Rule (spec FR) |
|---|---|---|---|
| `name` | `string` | Yes | non-empty after trim, ≤ 100 chars (FR-002) |
| `category` | `string` — one of `"Housing" \| "Services" \| "Transportation" \| "Subscriptions" \| "Other"` | Yes | one of the 5 fixed values (FR-003) |
| `monthlyAmount` | `number` (decimal) | Yes | `> 0`, ≤ 2 decimal places (FR-004) |
| `dueDay` | `integer` | Yes | `1..31` (FR-005) |
| `startDate` | `string`, ISO 8601 `yyyy-MM-dd` | Yes | valid calendar date (FR-006) |
| `frequency` | `string` — `"Monthly"` | Yes | only `"Monthly"` accepted this phase (FR-007) |
| `status` | `string` — `"Active" \| "Paused"` | Yes | defaults to `"Active"` if the UI's default is unchanged (FR-008) |
| `note` | `string \| null` | No | free text (FR-009) |

## Response — `201 Created`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Aluguel",
  "category": "Housing",
  "monthlyAmount": 1850.00,
  "dueDay": 10,
  "startDate": "2026-09-01",
  "frequency": "Monthly",
  "status": "Active",
  "note": null,
  "occurrences": [
    {
      "id": "b3fc2c96-4562-3fa8-5717-3f66afa65f64",
      "referencePeriod": { "year": 2026, "month": 9 },
      "dueDate": "2026-09-10",
      "status": "Pending",
      "expectedAmount": 1850.00,
      "name": "Aluguel",
      "category": "Housing"
    }
  ]
}
```

`occurrences` contains exactly 0 or 1 item: 1 when `status` sent was
`"Active"` (FR-010/spec Edge Cases), 0 when `"Paused"` (FR-010/US1-4). The
frontend only reads `name` from this response to populate the success
banner (FR-014); the rest of the payload is documented here for parity with
the domain contract, not because this screen renders it.

## Response — `400 Bad Request`

```json
{
  "errors": [
    { "field": "name", "message": "Nome é obrigatório." }
  ]
}
```

One item per violated business rule (FR-002–FR-008). `field` is expected to
match one of `name`, `category`, `monthlyAmount`, `dueDay`, `startDate` —
the same fields the client already validates locally (FR-016). The exact
error envelope shape (field names, structure) MUST be aligned with this
project's API-wide error format once one exists; none was found in this
repository as of this writing.

## Client handling contract

| Response | `formStatus` | Frontend behavior |
|---|---|---|
| `201 Created` | `success` | Show confirmation banner with `response.name` (FR-014); "Cadastrar outra despesa" resets the form. |
| `400 Bad Request`, `errors[i].field` matches a known client field | `error` | Show that field's inline error message (reusing the same rendering as client-side validation errors) in addition to the generic banner (FR-016). |
| `400 Bad Request`, no matching field, or network/`5xx` failure | `error` | Show only the generic error banner with "Tentar novamente" (FR-015, FR-016). |

No client-side timeout is applied while waiting for any response (spec
Clarification #3) — `formStatus` stays `loading` until the HTTP call
settles one way or the other.

## Out of scope

- Listing recurring expenses or occurrences (monthly panel).
- Editing, pausing/reactivating, or deleting an existing recurring expense.
- Marking an occurrence as paid or undoing a payment.
- Any endpoint supporting a frequency other than Monthly.
- Creating, editing, removing, or dynamically listing categories — the set
  is fixed on the client this iteration.
