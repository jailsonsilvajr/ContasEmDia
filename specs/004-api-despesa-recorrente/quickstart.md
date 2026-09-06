# Quickstart: API de Despesa Recorrente (Cadastro)

**Feature**: `004-api-despesa-recorrente` | **Date**: 2026-09-05

Guia de validação de ponta a ponta do endpoint
`POST /api/v1/recurring-expenses`. Ver [`contracts/api-contract.md`](./contracts/api-contract.md)
para a forma completa de request/response e [`data-model.md`](./data-model.md)
para os tipos citados.

## Pré-requisitos

- .NET 10 SDK instalado.
- Uma instância SQL Server acessível para desenvolvimento (ex.: LocalDB ou
  um container SQL Server), com a connection string configurada em
  `backend/Api/appsettings.Development.json` (nunca hardcoded — FR-014).
- Migrations do EF Core aplicadas (`dotnet ef database update`, a partir de
  `backend/Infrastructure`, apontando para `backend/Api` como projeto de
  inicialização).

## Executando a API localmente

```bash
cd backend/Api
dotnet run
```

A API sobe em `https://localhost:<porta>` (porta definida por
`launchSettings.json`, gerado pelo template padrão do ASP.NET Core Web API).

## Explorando via SwaggerUI (US4)

1. Com a API em execução, acesse `https://localhost:<porta>/swagger`.
2. Expanda `POST /api/v1/recurring-expenses`.
3. Confirme que os três retornos documentados (`201`, `400`, `500`)
   aparecem, cada um com o schema de `ApiResponse<CreateRecurringExpenseDataResponse>`.
4. Use "Try it out" para enviar o corpo de exemplo abaixo (US1) diretamente
   pela UI.

O documento OpenAPI machine-readable fica disponível em
`https://localhost:<porta>/swagger/v1/swagger.json`.

## Cenário 1 — Cadastro bem-sucedido, com ocorrência do mês corrente (US1, cenário 1)

```bash
curl -i -X POST https://localhost:<porta>/api/v1/recurring-expenses \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Aluguel",
    "category": "Housing",
    "monthlyAmount": 1850.00,
    "dueDay": 10,
    "startDate": "<primeiro dia do mês corrente, formato yyyy-MM-dd>",
    "frequency": "Monthly",
    "status": "Active",
    "note": null
  }'
```

**Resultado esperado**: `201 Created`, corpo
`ApiResponse<CreateRecurringExpenseDataResponse>` com `success: true` e
`data.occurrences` contendo exatamente 1 item, referente à competência
corrente.

## Cenário 2 — Cadastro bem-sucedido, sem ocorrência (US1, cenário 2)

Repita o cenário 1 com `"status": "Paused"` (ou `"status": "Active"` com
`startDate` em uma competência futura).

**Resultado esperado**: `201 Created`, `data.occurrences` como lista vazia
(`[]`).

## Cenário 3 — Falha de regra de negócio (US2)

```bash
curl -i -X POST https://localhost:<porta>/api/v1/recurring-expenses \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Aluguel",
    "category": "CategoriaInexistente",
    "monthlyAmount": 1850.00,
    "dueDay": 10,
    "startDate": "2026-09-01",
    "frequency": "Monthly",
    "status": "Active",
    "note": null
  }'
```

**Resultado esperado**: `400 Bad Request`, `success: false`, `data: null`,
`errors` com um item (`field: "category"`), mensagem em PT-BR.

## Cenário 4 — Falha de forma/presença (US3)

```bash
curl -i -X POST https://localhost:<porta>/api/v1/recurring-expenses \
  -H "Content-Type: application/json" \
  -d '{
    "category": "Housing",
    "monthlyAmount": 1850.00,
    "dueDay": 10,
    "startDate": "2026-09-01",
    "frequency": "Monthly",
    "status": "Active"
  }'
```

(campo `name` ausente do corpo)

**Resultado esperado**: `400 Bad Request`, mesmo formato de envelope do
Cenário 3, com `errors` contendo `{ "field": "name", "message": "Nome é obrigatório." }`
— nunca o formato `ValidationProblemDetails` nativo do ASP.NET Core.

## Cenário 5 — Falha inesperada (Edge Case, `500`)

Não há um comando direto para reproduzir este cenário manualmente (exige
simular a indisponibilidade do banco de dados, ex.: parando o SQL Server
antes da chamada). Cobertura automatizada deste cenário fica a cargo de
`Api.Tests` (ver Fase 0, item 6, `research.md`), forçando uma exceção na
camada de persistência.

## Rodando os testes automatizados

```bash
cd backend
dotnet test Api.Tests/ContasEmDia.Api.Tests.csproj
```

Cobre, no mínimo, um teste por cenário de resposta declarado por
`ProducesResponseType` no controller (Princípio II): sucesso com e sem
ocorrência, falha de negócio, falha de forma/presença e falha inesperada
(`500`).
