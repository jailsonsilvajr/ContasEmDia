---

description: "Task list template for feature implementation"
---

# Tasks: Provider de Banco de Dados PostgreSQL

**Input**: Design documents from `/specs/005-postgresql-provider/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md (all present)

**Tests**: Não solicitados explicitamente como TDD na spec. A troca reaproveita a suíte de testes de repositório já existente em `Infrastructure.Tests` (porta os mesmos casos para o novo fixture) — não são gerados novos casos de teste, apenas a infraestrutura de teste é trocada de provider (User Story 3).

**Organization**: Tarefas agrupadas por user story (spec.md) para permitir implementação e validação independentes de cada uma.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependência entre si)
- **[Story]**: US1, US2 ou US3 (mapeado das User Stories da spec.md)
- Caminhos de arquivo exatos incluídos em cada descrição

## Path Conventions

Projeto web já existente: `backend/<Projeto>/...` (ver plan.md, seção Project Structure). Esta feature não introduz nenhum projeto novo.

---

## Phase 1: Setup

**Purpose**: Nenhuma inicialização de projeto é necessária — a solução .NET já existe. Esta fase apenas resolve a informação externa (versão do PostgreSQL) que as fases seguintes precisam usar de forma consistente.

- [X] T001 Confirmar no Docker Hub qual é a tag `postgres:<major>-alpine` estável mais recente disponível no momento da implementação (ver research.md, seção "Decisão: Versão do PostgreSQL"); registrar essa tag para uso consistente nas tarefas T010 (Testcontainers), T014 (docker-compose.yml) e T020 (quickstart.md)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Trocar o provider EF Core de SQL Server para PostgreSQL na camada `Infrastructure` — pré-requisito bloqueante para as três user stories, já que nenhuma delas pode ser validada (aplicação, ambiente local ou testes) enquanto o provider ativo continuar sendo SQL Server.

**⚠️ CRITICAL**: Nenhuma user story pode começar antes desta fase estar completa.

- [X] T002 Substituir o pacote `Microsoft.EntityFrameworkCore.SqlServer` por `Npgsql.EntityFrameworkCore.PostgreSQL` (linha `10.x`, compatível com `Microsoft.EntityFrameworkCore.Design 10.0.11` já fixado) em `backend/Infrastructure/ContasEmDia.Infrastructure.csproj`
- [X] T003 Trocar `UseSqlServer(...)` por `UseNpgsql(...)` em `backend/Infrastructure/Contexts/ContasEmDiaDbContextFactory.cs`, atualizando a connection string placeholder de design-time para o formato Npgsql (`Host=localhost;Database=ContasEmDia;Username=postgres;Password=postgres`)
- [X] T004 [P] Trocar `.HasColumnType("decimal(18,2)")` por `.HasPrecision(18, 2)` na propriedade `_monthlyAmount` em `backend/Infrastructure/Configs/RecurringExpenseConfigurations.cs`
- [X] T005 [P] Trocar `.HasColumnType("decimal(18,2)")` por `.HasPrecision(18, 2)` na propriedade `_expectedAmount` em `backend/Infrastructure/Configs/OccurrenceConfigurations.cs`
- [X] T006 Remover a migração SQL Server `backend/Infrastructure/Migrations/20260831201615_InitialCreate.cs`, `backend/Infrastructure/Migrations/20260831201615_InitialCreate.Designer.cs` e `backend/Infrastructure/Migrations/ContasEmDiaDbContextModelSnapshot.cs` (depende de T002–T005 — a migração antiga deve ser removida só depois do provider/config já apontarem para Npgsql, para a próxima migração ser gerada corretamente)
- [X] T007 Gerar a nova migração inicial para o provider Npgsql (`dotnet ef migrations add InitialCreate --project backend/Infrastructure --startup-project backend/Api`), criando o novo `backend/Infrastructure/Migrations/<timestamp>_InitialCreate.cs`/`.Designer.cs` e `ContasEmDiaDbContextModelSnapshot.cs` com tipos PostgreSQL (`uuid`, `numeric(18,2)`, `date`, `character varying`); depende de T006 e de haver uma connection string Npgsql válida disponível em tempo de design (T003)

**Checkpoint**: Provider EF Core trocado e migração inicial regerada para PostgreSQL — as user stories podem começar.

---

## Phase 3: User Story 1 - Aplicação funciona de ponta a ponta sobre PostgreSQL (Priority: P1) 🎯 MVP

**Goal**: A aplicação persiste e recupera todos os dados existentes (despesas recorrentes e ocorrências) usando PostgreSQL, sem nenhuma mudança de comportamento observável pela API.

**Independent Test**: Subir a aplicação apontando para uma instância PostgreSQL, aplicar as migrações, e confirmar via os endpoints de API já existentes (ex.: cadastro de despesa recorrente) que criar, consultar, atualizar e remover dados funciona exatamente como antes.

### Implementation for User Story 1

- [X] T008 [US1] Trocar `UseSqlServer(...)` por `UseNpgsql(builder.Configuration.GetConnectionString("ContasEmDia"))` em `backend/Api/Program.cs`
- [X] T009 [US1] Atualizar `ConnectionStrings:ContasEmDia` em `backend/Api/appsettings.Development.json` para o formato Npgsql (`Host=localhost;Port=5432;Database=ContasEmDia;Username=postgres;Password=postgres`), removendo a string de conexão SQL Server

**Checkpoint**: Seguindo `quickstart.md` (passos 1–3, usando uma instância PostgreSQL disponível localmente), a API sobe sobre PostgreSQL, as migrações da Fase 2 são aplicadas com sucesso, e o endpoint de cadastro de despesa recorrente já existente funciona ponta a ponta (criar/consultar/atualizar/remover) com o mesmo comportamento observado anteriormente sobre SQL Server — User Story 1 entregue e validável de forma independente.

---

## Phase 4: User Story 2 - Ambiente de desenvolvimento local sem dependência de SQL Server (Priority: P2)

**Goal**: Um desenvolvedor consegue subir o ambiente local completo (aplicação + banco) usando apenas PostgreSQL via Docker, sem instalar ou licenciar SQL Server.

**Independent Test**: Seguir as instruções de setup do projeto em uma máquina limpa (sem SQL Server instalado), subir um PostgreSQL local via `docker-compose.yml`, aplicar as migrações e rodar a API com sucesso; inspecionar os arquivos de configuração e confirmar ausência de qualquer referência a SQL Server.

### Implementation for User Story 2

- [X] T010 [US2] Criar `docker-compose.yml` na raiz do repositório com um serviço `postgres` (imagem `postgres:<major>-alpine` conforme tag confirmada em T001), credenciais de desenvolvimento fixas (`postgres`/`postgres`, database `ContasEmDia`) e porta `5432` publicada, compatível com a connection string definida em T009
- [X] T011 [US2] Confirmar que `backend/Api/appsettings.json` (placeholder de produção) não contém nenhum valor ou comentário específico de SQL Server — mantém `ConnectionStrings:ContasEmDia` vazio, apenas com o formato Npgsql documentado em `quickstart.md`

**Checkpoint**: Em uma máquina sem SQL Server instalado, `docker compose up -d postgres` sobe o banco, `dotnet ef database update` (T007) aplica as migrações, e `dotnet run --project backend/Api` sobe a API com sucesso — User Story 2 entregue e validável de forma independente, além de User Story 1 continuar funcionando.

---

## Phase 5: User Story 3 - Testes automatizados validam o comportamento contra PostgreSQL real (Priority: P3)

**Goal**: A suíte de testes de persistência (`Infrastructure.Tests`) valida o comportamento dos repositórios contra uma instância PostgreSQL real descartável, em vez de SQL Server.

**Independent Test**: Rodar `dotnet test` no projeto `Infrastructure.Tests` e confirmar que ele sobe um container PostgreSQL descartável via Testcontainers, aplica as migrações nele, e executa todos os casos de teste existentes com sucesso — sem subir nenhum container SQL Server.

### Implementation for User Story 3

- [X] T012 [US3] Substituir o pacote `Testcontainers.MsSql` por `Testcontainers.PostgreSql` (linha `4.x`, mesma major já usada) em `backend/Infrastructure.Tests/ContasEmDia.Infrastructure.Tests.csproj`
- [X] T013 [US3] Renomear `backend/Infrastructure.Tests/SqlServerContainerFixture.cs` para `backend/Infrastructure.Tests/PostgreSqlContainerFixture.cs`: trocar `MsSqlContainer`/`MsSqlBuilder` por `PostgreSqlContainer`/`PostgreSqlBuilder` (imagem confirmada em T001), `UseSqlServer` por `UseNpgsql`, e renomear as classes `SqlServerContainerFixture`/`SqlServerCollection` para `PostgreSqlContainerFixture`/`PostgreSqlCollection` (depende de T012)
- [X] T014 [US3] Atualizar `[Collection(nameof(SqlServerCollection))]` para `[Collection(nameof(PostgreSqlCollection))]` em `backend/Infrastructure.Tests/Repositories/RecurringExpenseRepositoryTests.cs` (depende de T013)
- [X] T015 [US3] Rodar `dotnet test backend/Infrastructure.Tests` e confirmar que todos os testes de repositório existentes passam contra o container PostgreSQL descartável, com o mesmo resultado observado anteriormente contra SQL Server

**Checkpoint**: A suíte de testes de persistência roda inteiramente contra PostgreSQL real via container descartável, sem qualquer dependência de SQL Server — User Story 3 entregue e validável de forma independente, com User Story 1 e User Story 2 permanecendo funcionais.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Confirmar que a troca de provider está completa (FR-007) e que nada regrediu (SC-001, SC-004).

- [X] T016 [P] Rodar `grep -ril "sqlserver\|mssql" backend --include="*.csproj"` (quickstart.md, passo 5) e confirmar zero resultados, validando FR-007
- [X] T017 [P] Atualizar `research.md`, seção "Decisão: Versão do PostgreSQL", registrando a tag concreta confirmada em T001 como a efetivamente usada na implementação
- [X] T018 Rodar `dotnet test ContasEmDia.sln` (suíte completa: `Domain.Tests`, `Application.Tests`, `Infrastructure.Tests`, `Api.Tests`) e confirmar 100% de sucesso sem regressão, validando SC-001

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sem dependências — pode começar imediatamente.
- **Foundational (Phase 2)**: Depende de T001 (tag do PostgreSQL, usada em T007 indiretamente via ambiente de teste local do desenvolvedor). Bloqueia todas as user stories.
- **User Stories (Phase 3+)**: Todas dependem da Foundational (Phase 2) completa.
  - US1 (P1): pode começar assim que a Phase 2 terminar.
  - US2 (P2): pode começar em paralelo à US1 (usa a mesma connection string definida em T009 de US1 — na prática, aguardar T009 para evitar conflito no mesmo valor de configuração).
  - US3 (P3): independente de US1/US2 no código (mexe só em `Infrastructure.Tests`), mas depende da Phase 2 (migração já regerada) para os testes de repositório passarem.
- **Polish (Phase 6)**: Depende de todas as user stories desejadas estarem completas (T018 valida a suíte inteira, incluindo US3).

### User Story Dependencies

- **User Story 1 (P1)**: Depende apenas da Foundational (Phase 2). Sem dependência de US2/US3.
- **User Story 2 (P2)**: Depende da Foundational (Phase 2); reaproveita a connection string de US1 (T009) — recomendado completar US1 antes por já ser a prioridade maior, mas não há dependência técnica de código.
- **User Story 3 (P3)**: Depende da Foundational (Phase 2), especificamente da migração regerada em T007. Sem dependência de código de US1/US2.

### Within Each User Story

- T008 antes de validar US1 (Program.cs precisa apontar para Npgsql antes de a API subir).
- T009 antes do checkpoint de US1 e antes de T010 (docker-compose precisa ser compatível com a connection string).
- T012 → T013 → T014 → T015 em sequência estrita dentro de US3 (mesmo pacote/arquivo sendo trocado incrementalmente).

### Parallel Opportunities

- T004 e T005 podem rodar em paralelo (arquivos `Configs` diferentes).
- T016 e T017 podem rodar em paralelo (arquivos diferentes, sem dependência entre si).
- Após a Phase 2 completa, US1, US2 e US3 podem ser trabalhadas em paralelo por desenvolvedores diferentes (T008/T009 de US1, T010/T011 de US2, T012–T015 de US3 tocam arquivos distintos).

---

## Parallel Example: Foundational Phase

```bash
# Após T002/T003 (provider já trocado), rodar em paralelo:
Task: "Trocar .HasColumnType(\"decimal(18,2)\") por .HasPrecision(18, 2) em backend/Infrastructure/Configs/RecurringExpenseConfigurations.cs"
Task: "Trocar .HasColumnType(\"decimal(18,2)\") por .HasPrecision(18, 2) em backend/Infrastructure/Configs/OccurrenceConfigurations.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar Phase 1: Setup (T001).
2. Completar Phase 2: Foundational (T002–T007) — CRÍTICO, bloqueia tudo.
3. Completar Phase 3: User Story 1 (T008–T009).
4. **PARAR e VALIDAR**: seguir `quickstart.md` passos 1–3 contra uma instância PostgreSQL qualquer disponível (não precisa ser via docker-compose ainda).
5. Este é o MVP: aplicação funcionando ponta a ponta sobre PostgreSQL.

### Incremental Delivery

1. Setup + Foundational → provider trocado, migração regerada.
2. + User Story 1 → aplicação funciona sobre PostgreSQL (MVP).
3. + User Story 2 → ambiente local sem SQL Server, via `docker-compose.yml`.
4. + User Story 3 → suíte de testes valida contra PostgreSQL real.
5. Polish → confirma ausência total de SQL Server e suíte completa passando.

### Parallel Team Strategy

Com múltiplos desenvolvedores, após a Foundational (Phase 2) completa:

- Desenvolvedor A: User Story 1 (T008–T009).
- Desenvolvedor B: User Story 2 (T010–T011), aguardando T009 antes de finalizar T010.
- Desenvolvedor C: User Story 3 (T012–T015), sem dependência das outras duas.

---

## Notes

- [P] tasks = arquivos diferentes, sem dependência entre si.
- [Story] mapeia cada tarefa à user story correspondente para rastreabilidade.
- Nenhum teste novo é solicitado pela spec; User Story 3 reaproveita os casos de teste já existentes em `RecurringExpenseRepositoryTests.cs`, apenas trocando a infraestrutura de container (T012–T014).
- A remoção da migração SQL Server (T006) é a única exceção documentada ao Principle VII (ver plan.md, Complexity Tracking) — só deve ser feita depois de T002–T005, e imediatamente seguida da regeração em T007, para nunca deixar o histórico de migrações em estado inconsistente por mais que o necessário.
- Fazer commit após cada tarefa ou grupo lógico de tarefas.
- Parar em cada checkpoint para validar a user story de forma independente antes de seguir para a próxima.
