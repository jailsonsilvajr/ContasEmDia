# API Contract: recurso `occurrences`

**Feature**: `006-painel-mensal-despesas` | **Date**: 2026-09-07

Três endpoints novos, todos em `backend/Api/Controllers/OccurrencesController.cs`,
sob a rota `api/v1/occurrences`. Reaproveitam o envelope único
`ApiResponse<TData>`/`ApiError` já fixado em
[`004-api-despesa-recorrente`](../../004-api-despesa-recorrente/contracts/api-contract.md).
Ver [`data-model.md`](../data-model.md) para a forma completa de cada tipo
citado abaixo.

---

## `GET /api/v1/occurrences`

Lista as ocorrências de despesas recorrentes de uma competência (RF19–RF21).

### Request

Query string, ambos os parâmetros opcionais:

| Parâmetro | Tipo | Obrigatório | Efeito |
|---|---|---|---|
| `year` | string | não | Ano da competência. Deve vir acompanhado de `month`. |
| `month` | string | não | Mês da competência (1–12). Deve vir acompanhado de `year`. |

```text
GET /api/v1/occurrences                # competência atual (FR-002)
GET /api/v1/occurrences?year=2026&month=8
```

As setas de navegação de mês do painel (FR-020) reutilizam exatamente este
mesmo endpoint, apenas variando `year`/`month` para a competência de
destino — nenhum endpoint novo é necessário para a navegação entre meses.

### Response — `200 OK`

```json
{
  "success": true,
  "data": {
    "referencePeriod": { "year": 2026, "month": 8 },
    "occurrences": [
      {
        "id": "b3fc2c96-4562-3fa8-5717-3f66afa65f64",
        "name": "Aluguel",
        "category": "Housing",
        "expectedAmount": 1500.00,
        "dueDate": "2026-08-10",
        "status": "Pending",
        "paidAmount": null,
        "paymentDate": null
      },
      {
        "id": "1a2b3c4d-0000-1111-2222-333344445555",
        "name": "Netflix",
        "category": "Subscriptions",
        "expectedAmount": 55.90,
        "dueDate": "2026-08-08",
        "status": "Paid",
        "paidAmount": 59.90,
        "paymentDate": "2026-08-08"
      }
    ]
  },
  "errors": null
}
```

Tipo: `ApiResponse<GetMonthlyPanelDataResponse>` com `success: true`.
`data.occurrences` contém todas as ocorrências já geradas para a
competência, de despesas ativas ou pausadas (RF20), em qualquer ordem
(nenhuma ordenação é exigida — spec, "Assumptions"); pode ser uma lista
vazia (EC01). `status` é sempre o status **derivado** (`Paid`/`Overdue`/
`DueSoon`/`Pending`), calculado pelo servidor a partir da data completa de
vencimento comparada à data atual (RF22) — nunca o status persistido cru.
`paidAmount`/`paymentDate` são `null` exceto quando `status == "Paid"`.

Os banners (RF06–RF08) e os três totais (RF04–RF05) **não** vêm prontos
nesta resposta — o cliente os deriva somando/contando os itens já
classificados (RF23/CA13).

### Response — `400 Bad Request` (período inválido)

```json
{
  "success": false,
  "data": null,
  "errors": [
    { "field": "period", "message": "O mês deve estar entre 1 e 12." }
  ]
}
```

Cenários que produzem este erro (EC16/CA14): `month` fora de 1–12; `year`
ou `month` não numérico; apenas um dos dois parâmetros informado. Nunca
retorna dados de uma competência diferente da pedida nem trata o período
inválido como se fosse a competência atual.

### Response — `500 Internal Server Error`

Mesmo formato genérico já usado por todo endpoint existente (ver
`004-api-despesa-recorrente/contracts/api-contract.md`).

---

## `PATCH /api/v1/occurrences/{occurrenceId}/payment`

Marca uma ocorrência como paga (RF12–RF15, FR-010–FR-013).

### Request

```json
{
  "paidAmount": "1500,00",
  "paymentDate": "18/08/2026"
}
```

Tipo: `MarkOccurrenceAsPaidDataRequest`. Ambos os campos são **opcionais**
(sem `[Required]` — FR-013): ausentes, vazios (`""`/`null`) ou com texto
não numérico/não interpretável como data são aceitos sem erro de forma —
o servidor substitui silenciosamente pelo valor previsto da ocorrência
(`paidAmount`) e/ou pela data atual do servidor (`paymentDate`), nunca
bloqueando a confirmação (EC08–EC09).

### Response — `200 OK`

```json
{
  "success": true,
  "data": {
    "occurrence": {
      "id": "b3fc2c96-4562-3fa8-5717-3f66afa65f64",
      "name": "Aluguel",
      "category": "Housing",
      "expectedAmount": 1500.00,
      "dueDate": "2026-08-10",
      "status": "Paid",
      "paidAmount": 1500.00,
      "paymentDate": "2026-08-18"
    }
  },
  "errors": null
}
```

Tipo: `ApiResponse<MarkOccurrenceAsPaidDataResponse>` com `success: true`.

### Response — `400 Bad Request` (regra de negócio)

```json
{
  "success": false,
  "data": null,
  "errors": [
    { "field": null, "message": "Esta ocorrência já está paga." }
  ]
}
```

Produzido quando a ocorrência já está `Paid` no momento da chamada.
`field` é sempre `null` — não é um erro de um campo específico do corpo.

### Response — `404 Not Found`

```json
{
  "success": false,
  "data": null,
  "errors": [
    { "field": null, "message": "Ocorrência não encontrada." }
  ]
}
```

Produzido quando `occurrenceId` não corresponde a nenhuma ocorrência
existente.

### Response — `500 Internal Server Error`

Mesmo formato genérico.

---

## `DELETE /api/v1/occurrences/{occurrenceId}/payment`

Desfaz o pagamento de uma ocorrência (RF18, FR-017–FR-018).

### Request

Sem corpo. Apenas `occurrenceId` na rota.

### Response — `200 OK`

```json
{
  "success": true,
  "data": {
    "occurrence": {
      "id": "b3fc2c96-4562-3fa8-5717-3f66afa65f64",
      "name": "Aluguel",
      "category": "Housing",
      "expectedAmount": 1500.00,
      "dueDate": "2026-08-10",
      "status": "Overdue",
      "paidAmount": null,
      "paymentDate": null
    }
  },
  "errors": null
}
```

Tipo: `ApiResponse<UndoOccurrencePaymentDataResponse>` com `success: true`.
`status` já vem recalculado a partir da data de vencimento (EC11/CA10 —
pode voltar a ser `Overdue`, `DueSoon` ou `Pending`).

### Response — `400 Bad Request` (regra de negócio)

```json
{
  "success": false,
  "data": null,
  "errors": [
    { "field": null, "message": "Esta ocorrência ainda não foi paga." }
  ]
}
```

Produzido quando a ocorrência já está `Pending` no momento da chamada.

### Response — `404 Not Found`

Mesmo formato do `404` de `PATCH` acima, para `occurrenceId` inexistente.

### Response — `500 Internal Server Error`

Mesmo formato genérico.

---

## Documentação interativa

Documento OpenAPI machine-readable e SwaggerUI (já existentes, gerados por
`Swashbuckle.AspNetCore`) passam a cobrir também estes três endpoints, com
`ProducesResponseType` declarando todos os códigos documentados acima por
ação (`200`/`400`/`500` para `GET`; `200`/`400`/`404`/`500` para `PATCH` e
`DELETE`).

## Fora de escopo

- Qualquer endpoint de catálogo de categorias (RF24 — conjunto fechado já
  conhecido pelo cliente).
- Paginação e ordenação da listagem (spec, "Assumptions").
- Header `Location` nas respostas de `PATCH`/`DELETE` — não se aplica a
  ações que não criam um recurso novo.
- Rota com `{recurringExpenseId}` — o contrato usa apenas `occurrenceId`
  (ver `research.md` §4).
- Autenticação, autorização e política de CORS reais (débito explícito,
  Princípio IV).
- Validação de `occurrenceId` malformado (não-GUID) na rota — comportamento
  padrão do roteamento do ASP.NET Core, não coberto pela spec funcional
  desta feature.
