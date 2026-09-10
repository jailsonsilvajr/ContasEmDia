# Quickstart: Editar Despesa Recorrente (Backend)

**Feature**: `007-edit-recurring-expense` | **Date**: 2026-09-10

Guia de validação de ponta a ponta dos dois novos endpoints
`GET`/`PUT /api/v1/recurring-expenses/{id}`. Ver
[`contracts/api-contract.md`](./contracts/api-contract.md) para a forma
completa de request/response e [`data-model.md`](./data-model.md) para os
tipos citados. Pré-requisitos e execução local da API são os mesmos já
documentados em
[`specs/004-api-despesa-recorrente/quickstart.md`](../004-api-despesa-recorrente/quickstart.md)
(.NET 10 SDK, SQL Server acessível, migrations aplicadas, `dotnet run` a
partir de `backend/Api`).

## Preparando um registro para editar

Antes de qualquer cenário abaixo, cadastre uma despesa recorrente via
`POST /api/v1/recurring-expenses` (ver quickstart da feature 004) e guarde o
`id` devolvido — todos os cenários a seguir reusam esse `id`.

```bash
curl -s -X POST https://localhost:<porta>/api/v1/recurring-expenses \
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
  }' | jq -r '.data.id'
```

## Cenário 1 — Consultar os dados atuais (US2, CA08)

```bash
curl -i https://localhost:<porta>/api/v1/recurring-expenses/<id>
```

**Resultado esperado**: `200 OK`, `data` com todos os campos editáveis e
seus valores atuais, sem `occurrences`.

## Cenário 2 — Consultar um identificador inexistente (US2, CA08)

```bash
curl -i https://localhost:<porta>/api/v1/recurring-expenses/00000000-0000-0000-0000-000000000000
```

**Resultado esperado**: `404 Not Found`, envelope de erro com mensagem
"Despesa recorrente não encontrada.".

## Cenário 3 — Editar com sucesso, sem tocar ocorrências já geradas (US1, CA01/CA02)

```bash
curl -i -X PUT https://localhost:<porta>/api/v1/recurring-expenses/<id> \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Aluguel do apartamento",
    "category": "Housing",
    "monthlyAmount": 1900.00,
    "dueDay": 12,
    "startDate": "<mesma data de início atual>",
    "status": "Active",
    "note": "Reajuste anual"
  }'
```

**Resultado esperado**: `200 OK`, `data` refletindo os novos valores.
Confirme, consultando o painel mensal da competência corrente (fora do
escopo desta feature) ou o endpoint de ocorrências, que a ocorrência gerada
no cadastro continua com nome/categoria/valor antigos (CA02).

## Cenário 4 — Editar sem alterar nenhum campo (US1, CA07)

Repita o `PUT` do Cenário 3 enviando exatamente os mesmos valores já
salvos.

**Resultado esperado**: `200 OK`, `data` idêntico ao enviado; nenhuma
ocorrência nova, alterada ou removida.

## Cenário 5 — Pausar durante a edição (US1, CA03)

```bash
curl -i -X PUT https://localhost:<porta>/api/v1/recurring-expenses/<id> \
  -H "Content-Type: application/json" \
  -d '{ ... mesmos campos do Cenário 3, "status": "Paused" ... }'
```

**Resultado esperado**: `200 OK`, `data.status == "Paused"`; nenhuma
ocorrência já existente é alterada ou removida.

## Cenário 6 — Reativar durante a edição, gerando a ocorrência da competência atual (US3, CA04)

Com a despesa pausada pelo Cenário 5 (e sem ocorrência ainda para a
competência atual), reative:

```bash
curl -i -X PUT https://localhost:<porta>/api/v1/recurring-expenses/<id> \
  -H "Content-Type: application/json" \
  -d '{ ... mesmos campos do Cenário 3, "status": "Active" ... }'
```

**Resultado esperado**: `200 OK`, `data.status == "Active"`. Consultando o
painel mensal da competência atual (ou o endpoint de ocorrências), uma nova
ocorrência Pendente aparece com o valor previsto mensal vigente. Repetir o
mesmo `PUT` novamente (já `Active`) não deve gerar uma segunda ocorrência.

## Cenário 7 — Falha de regra de negócio (US4, CA01)

```bash
curl -i -X PUT https://localhost:<porta>/api/v1/recurring-expenses/<id> \
  -H "Content-Type: application/json" \
  -d '{ ... mesmos campos do Cenário 3, "monthlyAmount": -10 ... }'
```

**Resultado esperado**: `400 Bad Request`, `errors` com
`{ "field": "monthlyAmount", ... }`; nenhum campo é alterado.

## Cenário 8 — Falha de forma/presença (US4)

```bash
curl -i -X PUT https://localhost:<porta>/api/v1/recurring-expenses/<id> \
  -H "Content-Type: application/json" \
  -d '{ "category": "Housing", "monthlyAmount": 1900.00, "dueDay": 12, "startDate": "2026-09-01", "status": "Active" }'
```

(campo `name` ausente do corpo)

**Resultado esperado**: `400 Bad Request`, mesmo formato de envelope do
Cenário 7, com `{ "field": "name", "message": "Nome é obrigatório." }`.

## Cenário 9 — Editar um identificador inexistente (US4, CA09)

```bash
curl -i -X PUT https://localhost:<porta>/api/v1/recurring-expenses/00000000-0000-0000-0000-000000000000 \
  -H "Content-Type: application/json" \
  -d '{ ... corpo válido ... }'
```

**Resultado esperado**: `404 Not Found`, mesmo formato do Cenário 2; nenhuma
despesa nova é criada.

## Rodando os testes automatizados

```bash
cd backend
dotnet test Domain.Tests/ContasEmDia.Domain.Tests.csproj
dotnet test Application.Tests/ContasEmDia.Application.Tests.csproj
dotnet test Api.Tests/ContasEmDia.Api.Tests.csproj
```

Cobre, no mínimo: cada método novo do aggregate (`Domain.Tests`), sucesso/
não encontrado/falha de validação de cada UseCase novo
(`Application.Tests`), e um teste por cenário de resposta declarado nas
duas novas ações do controller (`Api.Tests`).
