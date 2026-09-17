# Quickstart: Validando "Data de Fim da Despesa Recorrente"

**Feature**: `009-data-fim-despesa-recorrente` | **Date**: 2026-09-17

Guia de validação ponta a ponta das cinco histórias de usuário da
`spec.md`. Pressupõe o ambiente de desenvolvimento já descrito pelas
features anteriores (PostgreSQL local via `deploy/`, API em
`https://localhost:{porta}`, frontend Angular em `http://localhost:4200`).

## Pré-requisitos

1. Backend com a migração desta feature aplicada (o backend já aplica
   migrações pendentes automaticamente no startup, per commit
   `77ca6fd`) — confirme no log de startup que a migração de `EndDate`
   rodou sem erro.
2. Frontend rodando (`npm start` em `frontend/`) apontando para a API local.
3. Data do sistema conhecida (as datas de exemplo abaixo assumem "hoje" =
   `2026-09-17`, mesma data desta sessão — ajuste as competências se
   validar em outro dia).

## Automatizado (rodar antes de qualquer validação manual)

```bash
cd backend
dotnet test                 # Domain.Tests, Application.Tests, Api.Tests, Infrastructure.Tests
cd ../frontend
npm test                    # cadastro/editar component specs + recurring-expense-form.util.spec.ts
```

Todos os testes novos desta feature (ver `research.md` §10) devem passar
antes de seguir para a validação manual abaixo — em particular o teste de
regressão de `Infrastructure.Tests` para remoção de ocorrência (§6), que só
é confiável contra PostgreSQL real, não contra o InMemory provider usado
por `Api.Tests`.

## User Story 1 — Cadastrar com data de fim (P1)

1. Abrir `/cadastro` no frontend.
2. Tentar salvar sem preencher "Data de fim" → confirmar bloqueio com
   mensagem de obrigatoriedade no campo (CA01).
3. Preencher "Data de início" = `2026-09-01`, "Data de fim" = `2026-09-01`
   (mesma data) → confirmar bloqueio "A data de fim deve ser posterior à
   data de início." (CA02).
4. Ajustar "Data de fim" = `2027-10-01` (mais de 1 ano após o início) →
   confirmar bloqueio de teto de vigência (CA03).
5. Ajustar "Data de fim" = `2026-12-01` (3 competências à frente), status
   Ativa, salvar → confirmar banner de sucesso e, no painel mensal, que as
   competências 09, 10, 11 e 12/2026 (4 ocorrências) já aparecem sem
   navegar mês a mês (CA04, CA05).
6. Repetir o passo 5 com status Pausada → confirmar as mesmas 4 ocorrências
   geradas, sem diferença de comportamento (CA04, EC05).

## User Story 2 — Estender a vigência (P2)

1. Editar a despesa criada no passo 5 acima (`endDate` atual =
   `2026-12-01`).
2. Alterar "Data de fim" para `2027-03-01` e salvar → confirmar que as
   competências 01, 02 e 03/2027 passam a existir no painel mensal, sem
   duplicar as de 09–12/2026 (CA06).
3. Cenário de lacuna retroativa: criar uma despesa com `endDate` já hoje
   (competência atual) — ex. `startDate=2026-09-01`, `endDate=2026-09-01`
   — depois editar `endDate` para uma competência futura, ex.
   `2026-12-01`, aguardando alguns segundos entre os dois passos (não é
   necessário simular passagem de mês real para este teste, já que a
   geração compara contra o antigo teto, não contra "hoje") → confirmar
   que as competências 10, 11 e 12/2026 são geradas mesmo que o teto
   anterior (09/2026) já coincida com a competência atual.
4. Tentar estender para uma data que viole o teto de 1 ano frente à
   `startDate` atual → confirmar bloqueio (CA03 reaplicado na edição, EC12).

## User Story 3 — Reduzir a vigência (P2)

1. Editar a despesa do passo 2 acima (vigência atual até `2027-03-01`,
   nenhuma ocorrência paga ainda).
2. Reduzir "Data de fim" para `2026-11-01` → confirmar que as ocorrências
   de 12/2026, 01/2027, 02/2027 e 03/2027 desaparecem do painel mensal
   (CA07).
3. Marcar como paga a ocorrência de 09/2026 (via painel mensal).
4. Tentar reduzir "Data de fim" para uma competência anterior a 09/2026
   (ex. `2026-08-01`) → confirmar rejeição total: nenhum campo é salvo,
   erro aparece no campo "Data de fim" (CA09).
5. Tentar reduzir "Data de fim" para uma competência já no passado frente
   ao mês atual (ex. `2026-08-01`, se hoje for `2026-09-17`) mesmo sem
   nenhuma ocorrência paga no intervalo → confirmar rejeição com mensagem
   "A data de fim não pode estar no passado." (FR-014, distinta da
   mensagem de CA09).

## User Story 4 — Consistência de dados antigos (P3)

1. Antes de aplicar a migração desta feature (ambiente de teste
   descartável, ou consultar um snapshot/backup pré-migração), identificar
   uma despesa recorrente já cadastrada com `startDate` conhecida.
2. Aplicar a migração (`dotnet ef database update` ou reiniciar a API, que
   aplica automaticamente).
3. Consultar a mesma despesa via `GET /api/v1/recurring-expenses/{id}` →
   confirmar `endDate == startDate + 1 ano` (CA08).
4. Confirmar que nenhuma ocorrência nova apareceu no painel mensal dessa
   despesa só por causa da migração (FR-011).

## Verificação de regressão de Infrastructure (research.md §6)

Executar manualmente (ou via teste automatizado equivalente) contra o
PostgreSQL real de desenvolvimento, não contra o provider InMemory:
1. Cadastrar uma despesa com vigência de 3 competências.
2. Editar, reduzindo a vigência para 1 competência (2 ocorrências devem
   ser removidas).
3. Consultar a tabela `Occurrences` diretamente (`psql` ou client SQL) e
   confirmar que as 2 linhas correspondentes foram de fato excluídas (não
   apenas desaparecidas da leitura via `GetByIdAsync`).

## Critério de conclusão

Todas as seções acima (User Stories 1–4 + verificação de Infrastructure)
validadas sem discrepância em relação aos critérios de aceitação (CA01–CA09)
e aos Success Criteria (SC-001–SC-005) da `spec.md`.
