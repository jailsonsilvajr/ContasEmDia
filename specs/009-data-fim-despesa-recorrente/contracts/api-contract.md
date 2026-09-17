# API Contract: Data de Fim da Despesa Recorrente

**Feature**: `009-data-fim-despesa-recorrente` | **Date**: 2026-09-17

Estende os dois endpoints já existentes de `RecurringExpensesController`
(`POST` e `PUT`) e o corpo de sucesso já retornado por `GET`. Nenhuma rota
nova, nenhum código de status novo, nenhuma mudança no envelope
`ApiResponse<TData>`/`ApiError` (`004-api-despesa-recorrente`).

## `POST /api/v1/recurring-expenses`

### Request — campo novo

```json
{
  "name": "Aluguel",
  "category": "Housing",
  "monthlyAmount": 1200.00,
  "dueDay": 5,
  "startDate": "2026-09-01",
  "endDate": "2027-03-01",
  "frequency": "Monthly",
  "status": "Active",
  "note": null
}
```

| Campo | Tipo | Obrigatório | Regra |
|---|---|---|---|
| `endDate` | `string` (`yyyy-MM-dd`) | Sim | Posterior a `startDate`; vigência (`endDate - startDate`) ≤ 1 ano; competência de `endDate` ≥ competência atual |

### Response 201 — campo novo

```json
{
  "success": true,
  "data": {
    "id": "…",
    "name": "Aluguel",
    "category": "Housing",
    "monthlyAmount": 1200.00,
    "dueDay": 5,
    "startDate": "2026-09-01",
    "endDate": "2027-03-01",
    "frequency": "Monthly",
    "status": "Active",
    "note": null,
    "occurrences": [
      { "id": "…", "referencePeriod": { "year": 2026, "month": 9 }, "dueDate": "2026-09-05", "status": "Pending", "expectedAmount": 1200.00, "name": "Aluguel", "category": "Housing" },
      { "id": "…", "referencePeriod": { "year": 2026, "month": 10 }, "dueDate": "2026-10-05", "status": "Pending", "expectedAmount": 1200.00, "name": "Aluguel", "category": "Housing" }
    ]
  },
  "errors": null
}
```

`occurrences` passa a conter uma entrada por competência entre a
competência atual e a de `endDate` (inclusive) — de 1 a ~13 itens — mesmo
quando `status` enviado é `"Paused"` (antes: no máximo 1 item, só quando
`Active`).

### Response 400 — novos casos de erro possíveis em `errors[]`

| `field` | `message` | Quando |
|---|---|---|
| `endDate` | "Data de fim é obrigatória." | Campo ausente/vazio (`[Required]`, model binding) |
| `endDate` | "Data de fim inválida." | Texto presente mas não é `yyyy-MM-dd` válido |
| `endDate` | "A data de fim deve ser posterior à data de início." | `endDate <= startDate` |
| `endDate` | "A vigência não pode ultrapassar 1 ano a partir da data de início." | `endDate > startDate + 1 ano` |
| `endDate` | "A data de fim não pode estar no passado." | competência de `endDate` anterior à competência atual |

Mesmo formato de erro já existente (`ApiResponse<CreateRecurringExpenseDataResponse>.Failure([...])`,
`400 Bad Request`); pode coexistir com erros de outros campos na mesma
resposta.

## `GET /api/v1/recurring-expenses/{id}`

### Response 200 — campo novo

```json
{
  "success": true,
  "data": {
    "id": "…",
    "name": "Aluguel",
    "category": "Housing",
    "monthlyAmount": 1200.00,
    "dueDay": 5,
    "startDate": "2026-09-01",
    "endDate": "2027-03-01",
    "frequency": "Monthly",
    "status": "Active",
    "note": null
  },
  "errors": null
}
```

`404` inalterado (despesa não encontrada).

## `PUT /api/v1/recurring-expenses/{id}`

### Request — campo novo

```json
{
  "name": "Aluguel",
  "category": "Housing",
  "monthlyAmount": 1200.00,
  "dueDay": 5,
  "startDate": "2026-09-01",
  "endDate": "2027-06-01",
  "status": "Active",
  "note": null
}
```

| Campo | Tipo | Obrigatório | Regra |
|---|---|---|---|
| `endDate` | `string` (`yyyy-MM-dd`) | Sim | Mesmas três regras do `POST`, avaliadas contra o `startDate` **efetivo** da requisição (já alterado, se também enviado) |

### Response 200 — campo novo

Mesma forma do `GET` acima (`RecurringExpenseDataResponse`, agora com
`endDate`). Efeito colateral (não visível no corpo desta resposta, apenas
no painel mensal): estender `endDate` cria ocorrências para as novas
competências cobertas (inclusive competências já passadas, se a vigência
anterior já estava vencida); reduzir `endDate` exclui as ocorrências das
competências que deixaram de ser cobertas.

### Response 400 — novos casos de erro possíveis em `errors[]`

| `field` | `message` | Quando |
|---|---|---|
| `endDate` | "Data de fim é obrigatória." | Campo ausente/vazio |
| `endDate` | "Data de fim inválida." | Texto malformado |
| `endDate` | "A data de fim deve ser posterior à data de início." | Cruzado com `startDate` efetivo |
| `endDate` | "A vigência não pode ultrapassar 1 ano a partir da data de início." | Cruzado com `startDate` efetivo |
| `endDate` | "A data de fim não pode estar no passado." | Competência de `endDate` anterior à atual |
| `endDate` | (mensagem de bloqueio por ocorrência paga, ex. "Não é possível reduzir a vigência: existe uma ocorrência já paga no período que seria removido.") | Redução excluiria ≥ 1 ocorrência já paga |
| `startDate` | "A data de fim deve ser posterior à data de início." / "A vigência não pode ultrapassar 1 ano a partir da data de início." | `startDate` editado de forma que viole a vigência contra o `endDate` vigente (efetivo) |

Quando a edição é rejeitada por qualquer uma das linhas acima, **nenhum**
campo da requisição é persistido — nem os que não tinham erro (`name`,
`category`, etc.) — mesma garantia de "tudo ou nada" já aplicada por
FR-009/FR-012 da `spec.md`.

`404` inalterado (despesa não encontrada).

## Sem mudança

- Envelope `ApiResponse<TData>` / `ApiError` (`004-api-despesa-recorrente`).
- Códigos de status possíveis por endpoint (`201`/`400`/`500` no `POST`;
  `200`/`404`/`500` no `GET`; `200`/`400`/`404`/`500` no `PUT`).
- Qualquer endpoint de `Occurrences` (marcar/desfazer pagamento).
