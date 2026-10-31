# Quickstart: Validando "Deletar Despesa Recorrente" (Backend)

**Feature**: `010-delete-recurring-expense` | **Date**: 2026-10-31

Guia de validação ponta a ponta das quatro histórias de usuário da
`spec.md`. Feature backend-only (sem tela dedicada ainda — frontend é
feature separada), então a validação manual usa chamadas HTTP diretas
contra a API local (`https://localhost:{porta}`), além dos testes
automatizados.

## Pré-requisitos

1. Backend com a migração desta feature (`AddRecurringExpenseDeletedAt`)
   aplicada — o backend já aplica migrações pendentes automaticamente no
   startup; confirme no log de startup que ela rodou sem erro.
2. Uma despesa recorrente já cadastrada via `POST
   /api/v1/recurring-expenses`, com ao menos 2 competências de vigência, e
   uma de suas ocorrências marcada como paga via `PATCH
   /api/v1/occurrences/{occurrenceId}/payment` (ver
   `specs/006-painel-mensal-despesas` para o fluxo de pagamento).

## Automatizado (rodar antes de qualquer validação manual)

```bash
cd backend
dotnet test   # Domain.Tests, Application.Tests, Api.Tests, Infrastructure.Tests
```

Todos os testes novos desta feature devem passar antes de seguir para a
validação manual abaixo — em particular o teste de integração de
`Infrastructure.Tests` que confirma o cascade delete real da abrangência
`all` (research.md §3), que só é confiável contra PostgreSQL real, não
contra o provider InMemory usado por `Api.Tests`.

## User Story 1 — Excluir preservando o histórico pago (P1)

Usando a despesa de pré-requisito (uma ocorrência paga, uma pendente):

1. `DELETE /api/v1/recurring-expenses/{id}?scope=pending-only` → confirmar
   `200 OK` com `data.scope == "pending-only"` (CA1).
2. `GET /api/v1/occurrences?year={ano}&month={mes}` (Painel Mensal) para a
   competência da ocorrência paga → confirmar que ela continua aparecendo,
   com `status` ainda refletindo o pagamento (CA1, CA5).
3. `GET /api/v1/occurrences?...` para a competência da ocorrência que
   estava pendente → confirmar que ela não aparece mais (CA1, CA4).
4. Avançar para uma competência futura (`?year=...&month=...` seguinte) →
   confirmar que nenhuma ocorrência nova desta despesa aparece (CA2).
5. `GET /api/v1/recurring-expenses/{id}` → confirmar `404 Not Found`
   (consulta direta trata como não encontrada, mesmo a despesa ainda
   existindo internamente — FR-007).

Repetir com uma despesa que só tem ocorrências pagas (nenhuma pendente) →
confirmar `200 OK`, nenhuma ocorrência removida, e o mesmo `404` em (5)
(CA3).

## User Story 2 — Excluir todo o histórico (P1)

Usando uma segunda despesa recorrente (ocorrências pagas e pendentes):

1. `DELETE /api/v1/recurring-expenses/{id}?scope=all` → confirmar `200 OK`
   com `data.scope == "all"` (CA1).
2. `GET /api/v1/recurring-expenses/{id}` → confirmar `404 Not Found` (CA2).
3. `GET /api/v1/occurrences?...` para qualquer competência em que esta
   despesa tinha ocorrência (paga ou não) → confirmar que nenhuma delas
   aparece mais (CA1, SC-002).

## User Story 3 — Rejeitar uma exclusão sem abrangência válida (P2)

Usando qualquer despesa recorrente existente e válida:

1. `DELETE /api/v1/recurring-expenses/{id}` (sem `scope`) → confirmar `400
   Bad Request`, `errors[0].field == "scope"`, e que a despesa continua
   acessível normalmente depois (CA1).
2. `DELETE /api/v1/recurring-expenses/{id}?scope=invalido` → confirmar o
   mesmo `400 Bad Request` e nenhuma alteração (CA2).

## User Story 4 — Identificador inexistente ou já excluído (P3)

1. `DELETE /api/v1/recurring-expenses/{um-guid-aleatorio-nunca-usado}?scope=all`
   → confirmar `404 Not Found` (CA1).
2. Reaproveitando a despesa já excluída com `scope=all` na User Story 2:
   `DELETE .../{id}?scope=pending-only` (abrangência diferente da primeira
   exclusão) → confirmar `404 Not Found` (CA2).
3. Reaproveitando a despesa já excluída com `scope=pending-only` na User
   Story 1: `GET /api/v1/recurring-expenses/{id}`, `PUT
   /api/v1/recurring-expenses/{id}` (qualquer corpo), e `DELETE
   .../{id}?scope=all` → confirmar `404 Not Found` nas três chamadas,
   apesar da despesa ainda existir internamente por preservar histórico
   pago (CA3).

## Verificação de regressão de Infrastructure (research.md §3–4)

Executar manualmente (ou via teste automatizado equivalente) contra o
PostgreSQL real de desenvolvimento, não contra o provider InMemory:

1. Excluir uma despesa com `scope=all` e confirmar via `psql` (ou client
   SQL) que a linha em `RecurringExpenses` e todas as linhas
   correspondentes em `Occurrences` foram de fato removidas — não apenas
   desaparecidas da leitura via `GetByIdAsync`.
2. Excluir uma despesa com `scope=pending-only` (com ocorrências pagas e
   pendentes) e confirmar via SQL que só as linhas das ocorrências não
   pagas foram removidas de `Occurrences`, a linha em `RecurringExpenses`
   permanece com `DeletedAt` preenchido, e as ocorrências pagas permanecem
   intactas.
3. Marcar uma ocorrência paga como alvo de `PATCH
   .../{occurrenceId}/payment` ou `DELETE .../{occurrenceId}/payment`
   (fluxo de pagamento) para uma despesa já excluída com
   `scope=pending-only` → confirmar que o fluxo de pagamento continua
   funcionando normalmente (não afetado por FR-007, que só se aplica à
   despesa recorrente em si — FR-008).

## Critério de conclusão

Todas as seções acima (User Stories 1–4 + verificação de Infrastructure)
validadas sem discrepância em relação aos critérios de aceitação da
`spec.md` e aos Success Criteria (SC-001–SC-005).
