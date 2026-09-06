# API Contract: `POST /api/v1/recurring-expenses`

**Feature**: `004-api-despesa-recorrente` | **Date**: 2026-09-05

**Status**: Contrato definitivo desta feature — substitui, na
implementação, a versão sem envelope/sem versionamento documentada em
[`specs/002-cadastro-despesa-recorrente/contracts/api-contract.md`](../../002-cadastro-despesa-recorrente/contracts/api-contract.md).
A atualização desse documento e da seção correspondente de
`refinements/frontend/cadastro-despesa-recorrente.md` é parte do trabalho de
implementação desta feature (FR-016) — ver `tasks.md`.

Único endpoint exposto por este projeto `Api`. Ver
[`data-model.md`](../data-model.md) para a forma completa de cada tipo
citado abaixo.

## Request

`POST /api/v1/recurring-expenses`

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

Tipo: `CreateRecurringExpenseDataRequest`. Todos os campos exceto `note` são
obrigatórios (validação de forma/presença apenas — nenhuma regra de negócio
é verificada nesta camada).

## Response — `201 Created`

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
  },
  "errors": null
}
```

Tipo: `ApiResponse<CreateRecurringExpenseDataResponse>` com `Success: true`.
`occurrences` contém exatamente 0 ou 1 item: 1 quando `status` enviado foi
`"Active"` com `startDate` na competência corrente, 0 quando `"Paused"` ou
`"Active"` com início em competência futura.

## Response — `400 Bad Request` (regra de negócio)

```json
{
  "success": false,
  "data": null,
  "errors": [
    { "field": "monthlyAmount", "message": "Valor previsto mensal deve ser maior que zero." }
  ]
}
```

Tipo: `ApiResponse<CreateRecurringExpenseDataResponse>` com `Success: false`.
Um item por campo que viola uma regra de negócio já fechada no Domain;
mensagens em PT-BR, relayed as-is a partir de `Output.Errors` do Use Case
(sem reescrita).

## Response — `400 Bad Request` (forma/presença)

```json
{
  "success": false,
  "data": null,
  "errors": [
    { "field": "name", "message": "Nome é obrigatório." }
  ]
}
```

Mesmo tipo e mesmo formato do `400` de regra de negócio acima — o chamador
não precisa distinguir a origem do erro. Nunca a resposta nativa
`ValidationProblemDetails` do ASP.NET Core.

## Response — `500 Internal Server Error`

```json
{
  "success": false,
  "data": null,
  "errors": [
    { "field": null, "message": "Ocorreu um erro inesperado. Tente novamente mais tarde." }
  ]
}
```

Produzida pelo `ExceptionHandlingMiddleware` global para qualquer exceção
não tratada (ex.: banco de dados indisponível) — nenhum detalhe interno da
exceção é exposto no corpo.

## Documentação interativa

Documento OpenAPI machine-readable e SwaggerUI expostos pela API (FR-012),
cobrindo este endpoint, `CreateRecurringExpenseDataRequest`,
`ApiResponse<CreateRecurringExpenseDataResponse>` e os três códigos de
retorno acima (`201`/`400`/`500`), declarados explicitamente via
`ProducesResponseType`.

## Fora de escopo

- Qualquer outro endpoint (listagem, detalhe, edição, pausa/reativação,
  exclusão, pagamento de ocorrência, catálogo de categorias via API).
- Header `Location` na resposta `201` — não existe endpoint de leitura
  (`GET`) para o qual apontar nesta etapa.
- Autenticação, autorização e política de CORS reais (débito explícito,
  Princípio IV).
