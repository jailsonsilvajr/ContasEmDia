# API Contract: `GET`/`PUT /api/v1/recurring-expenses/{id}`

**Feature**: `007-edit-recurring-expense` | **Date**: 2026-09-10

Dois novos endpoints, adicionados ao `RecurringExpensesController` já
existente (que hoje expõe apenas `POST /api/v1/recurring-expenses`, ver
[`specs/004-api-despesa-recorrente/contracts/api-contract.md`](../../004-api-despesa-recorrente/contracts/api-contract.md)).
Ver [`data-model.md`](../data-model.md) para a forma completa de cada tipo
citado abaixo.

## `GET /api/v1/recurring-expenses/{id}`

Consulta os dados atuais de uma despesa recorrente, para pré-carregar uma
edição (US2).

### Response — `200 OK`

```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Aluguel",
    "category": "Housing",
    "monthlyAmount": 1850.00,
    "dueDay": 10,
    "startDate": "2026-09-01",
    "frequency": "Monthly",
    "status": "Active",
    "note": null
  },
  "errors": null
}
```

Tipo: `ApiResponse<RecurringExpenseDataResponse>` com `Success: true`. Não
inclui `occurrences` (FR-013).

### Response — `404 Not Found`

```json
{
  "success": false,
  "data": null,
  "errors": [
    { "field": null, "message": "Despesa recorrente não encontrada." }
  ]
}
```

Produzida pelo `ExceptionHandlingMiddleware` global a partir de
`KeyNotFoundException` (FR-012).

### Response — `500 Internal Server Error`

Mesmo formato genérico já documentado para `POST` (erro inesperado, sem
detalhes internos expostos).

## `PUT /api/v1/recurring-expenses/{id}`

Salva a edição dos campos editáveis de uma despesa recorrente já cadastrada
(US1, US3, US4).

### Request

```json
{
  "name": "Aluguel do apartamento",
  "category": "Housing",
  "monthlyAmount": 1900.00,
  "dueDay": 12,
  "startDate": "2026-09-01",
  "status": "Active",
  "note": "Reajuste anual"
}
```

Tipo: `UpdateRecurringExpenseDataRequest`. Todos os campos exceto `note` são
obrigatórios (validação de forma/presença apenas). Não inclui `frequency`
(não editável — FR-001).

### Response — `200 OK`

```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Aluguel do apartamento",
    "category": "Housing",
    "monthlyAmount": 1900.00,
    "dueDay": 12,
    "startDate": "2026-09-01",
    "frequency": "Monthly",
    "status": "Active",
    "note": "Reajuste anual"
  },
  "errors": null
}
```

Tipo: `ApiResponse<RecurringExpenseDataResponse>` com `Success: true` — os
mesmos campos e o mesmo tipo do `GET` acima, já atualizados (FR-017). Não
`201` (nenhum recurso novo é criado).

### Response — `400 Bad Request` (regra de negócio)

```json
{
  "success": false,
  "data": null,
  "errors": [
    { "field": "monthlyAmount", "message": "O valor monetário deve ser maior que zero." }
  ]
}
```

Um item por campo que viola uma regra de negócio já fechada no Domain
(FR-016); nada é salvo (nenhum método do aggregate é chamado quando há
qualquer erro).

### Response — `400 Bad Request` (forma/presença)

Mesmo tipo e mesmo formato do `400` de regra de negócio acima (FR-020) —
ex.: `{ "field": "name", "message": "Nome é obrigatório." }` — nunca o
formato `ValidationProblemDetails` nativo do ASP.NET Core.

### Response — `404 Not Found`

```json
{
  "success": false,
  "data": null,
  "errors": [
    { "field": null, "message": "Despesa recorrente não encontrada." }
  ]
}
```

Identificador da rota não corresponde a nenhuma despesa recorrente
cadastrada (FR-015); nenhuma despesa nova é criada.

### Response — `500 Internal Server Error`

Mesmo formato genérico já documentado para `POST`.

## Reativação como efeito colateral do `PUT` (US3, FR-005–FR-007/FR-018)

Quando o `status` enviado muda de `"Paused"` para `"Active"`, a mesma
chamada de `PUT` avalia e, se as condições forem atendidas (data de início
já começou e não existe ocorrência da despesa para a competência atual),
gera uma ocorrência Pendente para a competência atual — sem nenhuma chamada
adicional. Esse efeito não aparece na resposta do `PUT` (que só devolve os
campos de `RecurringExpenseDataResponse`, sem a lista de ocorrências,
igual ao `GET`); para confirmar a ocorrência gerada, o chamador consulta o
painel mensal (`painel-mensal-despesas.md`, fora do escopo desta feature) ou
o endpoint de ocorrências já existente.

## Documentação interativa

Documento OpenAPI machine-readable e SwaggerUI expostos pela API (já
existentes desde a feature 004) passam a cobrir também estes dois novos
endpoints, `UpdateRecurringExpenseDataRequest`,
`ApiResponse<RecurringExpenseDataResponse>` e todos os códigos de retorno
acima, declarados via `ProducesResponseType`.

## Fora de escopo

- Exclusão de despesa recorrente.
- Qualquer alteração em ocorrências individuais (`OccurrencesController`,
  inalterado).
- A lista de ocorrências não é retornada por nenhum dos dois novos
  endpoints (FR-013) — apenas os campos editáveis da despesa recorrente.
- Autenticação, autorização e política de CORS reais (débito explícito,
  Princípio IV, já vigente para os endpoints existentes).
