# API Contract: `DELETE /api/v1/recurring-expenses/{id}`

**Feature**: `010-delete-recurring-expense` | **Date**: 2026-10-31

Uma nova ação, adicionada ao `RecurringExpensesController` já existente (que
hoje expõe `POST`, `GET {id}` e `PUT {id}` — ver
[`specs/007-edit-recurring-expense/contracts/api-contract.md`](../../007-edit-recurring-expense/contracts/api-contract.md)).
Ver [`data-model.md`](../data-model.md) para a forma completa de cada tipo
citado abaixo.

## `DELETE /api/v1/recurring-expenses/{id}?scope={pending-only|all}`

Exclui definitivamente uma despesa recorrente já cadastrada, na abrangência
escolhida (US1, US2, US3, US4). Sem corpo de requisição — `scope` é o único
parâmetro, via query string, seguindo o mesmo precedente do único outro
`DELETE` já implementado no projeto
(`DELETE /api/v1/occurrences/{occurrenceId}/payment`,
`OccurrencesController.UndoPayment`).

### Response — `200 OK` (abrangência `pending-only`)

```json
{
  "success": true,
  "data": {
    "recurringExpenseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "scope": "pending-only"
  },
  "errors": null
}
```

Tipo: `ApiResponse<DeleteRecurringExpenseDataResponse>` com `success: true`.
Toda ocorrência não paga desta despesa foi removida; toda ocorrência paga
permanece intacta e acessível (FR-003); a despesa nunca mais gera uma nova
ocorrência (FR-003) e nunca mais é encontrada por `GET`/`PUT`/`DELETE`
diretos neste mesmo id (FR-007) — mas continua aparecendo normalmente no
Painel Mensal e no fluxo de pagamento para as competências em que já tinha
ocorrência paga (FR-008).

### Response — `200 OK` (abrangência `all`)

```json
{
  "success": true,
  "data": {
    "recurringExpenseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "scope": "all"
  },
  "errors": null
}
```

Tipo: `ApiResponse<DeleteRecurringExpenseDataResponse>` com `success: true`.
Absolutamente todas as ocorrências desta despesa — pagas ou não — e a
própria despesa recorrente deixam de existir (FR-004); uma nova consulta por
este id, em qualquer endpoint (`GET`/`PUT`/`DELETE`, Painel Mensal, fluxo de
pagamento), não encontra mais nada relacionado a ela (SC-002).

Não `204 No Content` (mesmo precedente de `UndoPayment`: sempre `200 OK` com
o envelope `ApiResponse<TData>` preenchido).

### Response — `400 Bad Request` (abrangência ausente ou inválida)

```json
{
  "success": false,
  "data": null,
  "errors": [
    { "field": "scope", "message": "Informe a abrangência da exclusão: \"pending-only\" ou \"all\"." }
  ]
}
```

Produzida pelo próprio `DeleteRecurringExpenseUseCase` antes de qualquer
acesso ao repositório (FR-002); mesmo formato de erro por campo já usado
pelos demais endpoints — nunca o `ValidationProblemDetails` nativo do
ASP.NET Core, e nunca via uma anotação de `[Required]`/`[RegularExpression]`
em um `DataRequest`, já que esta ação não tem corpo nem `DataRequest`
dedicado (mesmo caso do `GET {id}` bodyless).

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
`KeyNotFoundException`, lançada pelo `DeleteRecurringExpenseUseCase` quando
o id nunca existiu (FR-006), ou já foi excluído anteriormente em qualquer
uma das duas abrangências (FR-007) — mesmo mecanismo que
`GetRecurringExpenseByIdUseCase`/`UpdateRecurringExpenseUseCase` já usam,
sem nenhum código novo de tratamento de erro.

### Response — `500 Internal Server Error`

Mesmo formato genérico já documentado para os demais endpoints (erro
inesperado, sem detalhes internos expostos).

## Documentação interativa

O documento OpenAPI machine-readable e o SwaggerUI já expostos pela API
passam a cobrir também esta nova ação, `DeleteRecurringExpenseDataResponse`,
e todos os códigos de retorno acima (`200`, `400`, `404`, `500`), declarados
via `ProducesResponseType`.

## Fora de escopo

- Qualquer alteração em `OccurrencesController` ou nos UseCases de
  pagamento (`MarkOccurrenceAsPaid`/`UndoOccurrencePayment`) — permanecem
  inalterados; a exclusão independe de status e não introduz regra nova
  sobre pagamento de ocorrência já existente.
- Mecanismo de "desfazer exclusão" ou auditoria de quem excluiu e quando.
- O ponto de entrada de UI (tela/botão) de onde este `DELETE` é acionado —
  feature de frontend separada.
- Autenticação, autorização e política de CORS reais (débito explícito,
  Princípio IV, já vigente para os endpoints existentes).
