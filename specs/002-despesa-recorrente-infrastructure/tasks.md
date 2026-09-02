---

description: "Task list template for feature implementation"
---

# Tasks: Infrastructure de Despesa Recorrente (Persistência)

**Input**: Design documents from `/specs/002-despesa-recorrente-infrastructure/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/repository-contract.md, quickstart.md

**Tests**: Included — plan.md's Constitution Check (Principle II — Test-First Development) requires persistence integration tests to be written before repository/DbContext implementation, covering all 3 User Stories and the edge cases of FR-001–FR-009.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Solução backend existente: `backend/ContasEmDia.sln`. Novo projeto de produção `backend/Infrastructure/`, novo projeto de testes de integração `backend/Infrastructure.Tests/`. `backend/Domain/` já existe e é apenas estendido (não recriado) por esta feature.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Criar os dois novos projetos (`Infrastructure`, `Infrastructure.Tests`) e integrá-los à solução existente.

- [X] T001 Criar o projeto `backend/Infrastructure/ContasEmDia.Infrastructure.csproj` (SDK `Microsoft.NET.Sdk`, `TargetFramework net10.0`, `Nullable enable`, `TreatWarningsAsErrors true` — mesmo padrão de `backend/Domain/ContasEmDia.Domain.csproj`), com `ProjectReference` para `backend/Domain/ContasEmDia.Domain.csproj` e `PackageReference` para `Microsoft.EntityFrameworkCore.SqlServer` (10.x) e `Microsoft.EntityFrameworkCore.Design` (10.x)
- [X] T002 [P] Criar o projeto `backend/Infrastructure.Tests/ContasEmDia.Infrastructure.Tests.csproj` (mesmo padrão de `backend/Domain.Tests/ContasEmDia.Domain.Tests.csproj`, `TargetFramework net10.0`, `Nullable enable`, `TreatWarningsAsErrors true`), com `ProjectReference` para `backend/Infrastructure/ContasEmDia.Infrastructure.csproj` e `PackageReference` para `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` e `Testcontainers.MsSql`
- [X] T003 Adicionar os projetos `Infrastructure` e `Infrastructure.Tests` (e suas pastas de solution) a `backend/ContasEmDia.sln`, seguindo a mesma estrutura de nesting já usada por `Domain`/`Domain.Tests` (depende de T001, T002)

**Checkpoint**: Solução restaura (`dotnet restore`) e compila (`dotnet build backend/ContasEmDia.sln`) com os dois projetos vazios adicionados.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Infraestrutura central que TODAS as user stories precisam — construtores de reconstrução no Domain (Princípio VI), `DbContext`, mapeamentos EF Core, repositório (stub) e `RepositoryManager`, migration inicial e fixture de testes contra SQL Server real.

**⚠️ CRITICAL**: Nenhuma user story pode ser implementada/testada antes desta fase estar completa — o `RecurringExpenseRepository` stub garante que a interface `IRecurringExpenseRepository` compile desde já, e cada user story substitui um método do stub por sua implementação real.

- [X] T004 [P] Adicionar a `RecurringExpense` (`backend/Domain/Aggregates/RecurringExpense.cs`) um construtor privado de reconstrução que recebe `Guid id`, todos os Value Objects (`ExpenseName`, `ExpenseCategory`, `Money`, `DueDay`, `CalendarDate`, `Frequency`, `RecurringExpenseStatus`, `Note`) já validados e a coleção de `Occurrence` já materializada, apenas atribuindo os campos — sem a lógica condicional de geração de ocorrência do construtor público existente (FR-009, data-model.md §Reconstrução, research.md §1)
- [X] T005 [P] Adicionar a `Occurrence` (`backend/Domain/Entities/Occurrence.cs`) um construtor privado de reconstrução que recebe `Guid id`, `ReferencePeriod`, `CalendarDate dueDate`, `OccurrenceStatus status`, `ExpenseName`, `ExpenseCategory`, `Money expectedAmount` já validados, apenas atribuindo os campos — sem gerar novo `Guid` nem forçar `OccurrenceStatusType.Pending` como faz o construtor `internal` existente (FR-009, data-model.md §Reconstrução, research.md §1)
- [X] T006 [P] Criar `ContasEmDiaDbContext` em `backend/Infrastructure/Contexts/ContasEmDiaDbContext.cs`: construtor recebendo `DbContextOptions<ContasEmDiaDbContext>` já configuradas externamente, expondo `DbSet<RecurringExpense> RecurringExpenses => Set<RecurringExpense>()` e sobrescrevendo `OnModelCreating` para chamar `modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContasEmDiaDbContext).Assembly)` (contracts/repository-contract.md)
- [X] T007 [P] Criar `RecurringExpenseConfigurations` (`IEntityTypeConfiguration<RecurringExpense>`) em `backend/Infrastructure/Configs/RecurringExpenseConfigurations.cs`: chave primária `Id` (`uniqueidentifier`, via `GetId()`), `HasConversion` para `Name`→`nvarchar(200)`, `Category`→`int`, `MonthlyAmount`→`decimal(18,2)`, `DueDay`→`int`, `StartDate`→`date`, `Frequency`→`int`, `Status`→`int`, `Note`→`nvarchar(1000)` nulável; mapear a coleção privada `_occurrences` para a navegação `Occurrences` via `UsePropertyAccessMode(PropertyAccessMode.Field)` (data-model.md §Tabela RecurringExpenses) — depende de T004 (usa o construtor de reconstrução para materialização)
- [X] T008 [P] Criar `OccurrenceConfigurations` (`IEntityTypeConfiguration<Occurrence>`) em `backend/Infrastructure/Configs/OccurrenceConfigurations.cs`: chave primária `Id` (via `GetId()`), FK obrigatória `RecurringExpenseId` (`uniqueidentifier NOT NULL`) para `RecurringExpenses.Id`, colunas `ReferenceYear`/`ReferenceMonth` (`int`, de `GetReferencePeriod()`), `HasConversion` para `DueDate`→`date`, `Status`→`int`, `Name`→`nvarchar(200)`, `Category`→`int`, `ExpectedAmount`→`decimal(18,2)` (data-model.md §Tabela Occurrences) — depende de T005 (usa o construtor de reconstrução para materialização)
- [X] T009 Criar `RecurringExpenseRepository` (stub inicial) em `backend/Infrastructure/Repositories/RecurringExpenseRepository.cs`: implementa `IRecurringExpenseRepository`, construtor recebendo `ContasEmDiaDbContext`, com `AddAsync`, `GetByIdAsync` e `GetActiveAsync` lançando `NotImplementedException` — garante que a interface compile antes de cada user story substituir seu método correspondente (depende de T006, T007, T008)
- [X] T010 Criar `RepositoryManager` em `backend/Infrastructure/RepositoryManager.cs`: `sealed class` com `Lazy<IRecurringExpenseRepository> _recurringExpenseRepository` inicializado no construtor (recebendo `ContasEmDiaDbContext`) para `new RecurringExpenseRepository(context)`, expondo a propriedade `RecurringExpenseRepository => _recurringExpenseRepository.Value` (FR-012, contracts/repository-contract.md) — depende de T009
- [X] T011 Gerar a migration inicial em `backend/Infrastructure/Migrations/` com `dotnet ef migrations add InitialCreate --project Infrastructure --startup-project Infrastructure`, validando que o schema gerado contém `RecurringExpenses`, `Occurrences` e a FK `Occurrences.RecurringExpenseId → RecurringExpenses.Id` (FR-013, data-model.md) — depende de T006, T007, T008
- [X] T012 [P] Criar a fixture de testes contra SQL Server real em `backend/Infrastructure.Tests/SqlServerContainerFixture.cs` (usando `Testcontainers.MsSql`, `IAsyncLifetime`) + `CollectionDefinition`/`ICollectionFixture` para compartilhar um único container descartável entre as suítes, aplicando `context.Database.MigrateAsync()` antes dos testes (research.md §3) — depende de T002, T011

**Checkpoint**: `dotnet build backend/ContasEmDia.sln` compila sem warnings; a fixture de testes consegue subir o container, aplicar a migration e abrir uma conexão. A partir daqui as user stories podem ser implementadas.

---

## Phase 3: User Story 1 - Salvar despesa recorrente cadastrada, com a ocorrência do mês corrente (Priority: P1) 🎯 MVP

**Goal**: Persistir, em uma única confirmação de escrita, uma `RecurringExpense` e — quando existente — sua `Occurrence` do mês corrente; garantir rollback completo em caso de falha.

**Independent Test**: Criar via Domain uma `RecurringExpense` Ativa com ocorrência do mês corrente gerada, salvá-la via `RecurringExpenseRepository.AddAsync`, e verificar que ambas aparecem persistidas de forma consistente (ou nenhuma das duas, em caso de falha simulada).

### Tests for User Story 1 ⚠️

> **NOTE: Escrever estes testes PRIMEIRO, garantir que falhem antes de implementar T016**

- [X] T013 [US1] Teste de integração em `backend/Infrastructure.Tests/Repositories/RecurringExpenseRepositoryTests.cs`: salvar uma `RecurringExpense` Ativa com ocorrência do mês corrente gerada → despesa e ocorrência persistidas em uma única confirmação de escrita (Acceptance Scenario 1, FR-001)
- [X] T014 [US1] Teste de integração (mesmo arquivo `RecurringExpenseRepositoryTests.cs`): salvar uma `RecurringExpense` Pausada (ou Ativa com início futuro) sem ocorrência gerada → apenas a despesa é persistida, sem nenhuma ocorrência associada (Acceptance Scenario 2, FR-003) — depende de T013 (mesmo arquivo)
- [X] T015 [US1] Teste de integração (mesmo arquivo `RecurringExpenseRepositoryTests.cs`): forçar uma falha na escrita (ex.: violação de restrição, como inserir uma `Occurrence` duplicada/órfã diretamente via `DbContext`) → nem a despesa nem a ocorrência ficam persistidas, a operação falha por completo (Acceptance Scenario 3, FR-002) — depende de T014 (mesmo arquivo)

### Implementation for User Story 1

- [X] T016 [US1] Implementar `AddAsync(RecurringExpense)` em `backend/Infrastructure/Repositories/RecurringExpenseRepository.cs` (substituindo o stub): `_context.RecurringExpenses.Add(recurringExpense)` seguido de uma única chamada a `SaveChangesAsync()`, sem nenhuma lógica de negócio adicional (FR-001, FR-002, FR-010) — depende de T013, T014, T015 (testes devem existir e falhar antes)

**Checkpoint**: User Story 1 completa e testável de forma independente — despesas recorrentes cadastradas sobrevivem à persistência (MVP).

---

## Phase 4: User Story 2 - Recuperar despesa recorrente por identificador (Priority: P2)

**Goal**: Localizar uma `RecurringExpense` persistida pelo seu `Guid`, retornando-a com todas as suas `Occurrence`s, ou `null` se não existir.

**Independent Test**: Salvar uma `RecurringExpense` com uma ocorrência (via US1) e recuperá-la pelo identificador, verificando que os dados e ocorrências retornam intactos; verificar que um identificador inexistente retorna `null` sem lançar erro.

### Tests for User Story 2 ⚠️

> **NOTE: Escrever estes testes PRIMEIRO, garantir que falhem antes de implementar T019**

- [X] T017 [US2] Teste de integração em `backend/Infrastructure.Tests/Repositories/RecurringExpenseRepositoryTests.cs`: buscar por um `Guid` de despesa persistida com ocorrências associadas → despesa retornada junto de todas as suas ocorrências intactas (Acceptance Scenario 1, FR-004)
- [X] T018 [US2] Teste de integração (mesmo arquivo `RecurringExpenseRepositoryTests.cs`): buscar por um `Guid` inexistente → retorno `null`, sem lançar exceção (Acceptance Scenario 2, FR-005) — depende de T017 (mesmo arquivo)

### Implementation for User Story 2

- [X] T019 [US2] Implementar `GetByIdAsync(Guid id)` em `backend/Infrastructure/Repositories/RecurringExpenseRepository.cs` (substituindo o stub): `_context.RecurringExpenses.Include(e => e.Occurrences).FirstOrDefaultAsync(e => e.GetId() == id)`, retornando `null` quando não encontrado (FR-004, FR-005) — depende de T017, T018 (testes devem existir e falhar antes)

**Checkpoint**: User Stories 1 e 2 funcionam de forma independente.

---

## Phase 5: User Story 3 - Listar despesas recorrentes ativas (Priority: P3)

**Goal**: Localizar todas as `RecurringExpense` com status Ativa, cada uma com suas `Occurrence`s.

**Independent Test**: Salvar despesas recorrentes com status misto (Ativa/Pausada) e verificar que a listagem retorna exclusivamente as Ativas, cada uma com suas ocorrências; verificar que a ausência de despesas ativas retorna lista vazia.

### Tests for User Story 3 ⚠️

> **NOTE: Escrever estes testes PRIMEIRO, garantir que falhem antes de implementar T022**

- [X] T020 [US3] Teste de integração em `backend/Infrastructure.Tests/Repositories/RecurringExpenseRepositoryTests.cs`: dado um conjunto de despesas persistidas com status misto → a listagem de ativas retorna somente as com status Ativa, cada uma com suas ocorrências (Acceptance Scenario 1, FR-006)
- [X] T021 [US3] Teste de integração (mesmo arquivo `RecurringExpenseRepositoryTests.cs`): nenhuma despesa ativa persistida → listagem retorna lista vazia, sem lançar exceção (Acceptance Scenario 2, FR-007) — depende de T020 (mesmo arquivo)

### Implementation for User Story 3

- [X] T022 [US3] Implementar `GetActiveAsync()` em `backend/Infrastructure/Repositories/RecurringExpenseRepository.cs` (substituindo o stub): `_context.RecurringExpenses.Include(e => e.Occurrences).Where(e => e.GetStatus().GetValue() == RecurringExpenseStatusType.Active).ToListAsync()` (FR-006, FR-007) — depende de T020, T021 (testes devem existir e falhar antes)

**Checkpoint**: Todas as user stories funcionam de forma independente — contrato `IRecurringExpenseRepository` totalmente implementado.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validações finais que cobrem toda a feature.

- [X] T023 [P] Confirmar que `dotnet build backend/ContasEmDia.sln` conclui sem warnings (Domain, Infrastructure e Infrastructure.Tests, todos com `TreatWarningsAsErrors`)
- [ ] T024 Executar a validação manual de reconstrução do quickstart.md (§Validação manual rápida): salvar uma `RecurringExpense` Ativa com ocorrência via `AddAsync`, recuperá-la via `GetByIdAsync` em uma nova instância de `ContasEmDiaDbContext`, e comparar todos os campos (despesa e ocorrência) com os originais (SC-002)
- [X] T025 [P] Confirmar que nenhuma connection string ou segredo aparece hardcoded em código-fonte versionado dentro de `backend/Infrastructure/` (FR-011, SC-005) — apenas `DbContextOptions` recebidas externamente

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sem dependências — pode começar imediatamente
- **Foundational (Phase 2)**: Depende da conclusão do Setup — BLOQUEIA todas as user stories
- **User Stories (Phase 3-5)**: Todas dependem da conclusão da Foundational
  - US1 (P1) → US2 (P2) → US3 (P3) em ordem de prioridade, todas editando o mesmo arquivo `RecurringExpenseRepository.cs` (o stub de T009 é substituído método a método) — não são paralelizáveis entre si por conflito de arquivo, mas cada uma é independentemente testável assim que sua implementação está pronta
- **Polish (Phase 6)**: Depende da conclusão de todas as user stories desejadas

### User Story Dependencies

- **User Story 1 (P1)**: Pode começar após a Foundational (Phase 2) — sem dependência de outras stories
- **User Story 2 (P2)**: Pode começar após a Foundational; compartilha o arquivo `RecurringExpenseRepository.cs` com US1 (edição sequencial), mas é testável de forma independente
- **User Story 3 (P3)**: Pode começar após a Foundational; compartilha o arquivo `RecurringExpenseRepository.cs` com US1/US2 (edição sequencial), mas é testável de forma independente

### Within Each User Story

- Testes de integração MUST ser escritos e FALHAR antes da implementação (Princípio II)
- Testes dentro do mesmo arquivo (`RecurringExpenseRepositoryTests.cs`) são sequenciais, não paralelos
- Implementação do método do repositório só após os testes da story existirem

### Parallel Opportunities

- T001 e T002 (Setup) em paralelo
- T004, T005, T006, T007, T008 (Foundational) em paralelo entre si (arquivos distintos), respeitando que T007 depende de T004 e T008 depende de T005
- T012 (fixture de testes) em paralelo com T009/T010/T011 (arquivos distintos), mas depende de T002 e T011
- T023 e T025 (Polish) em paralelo

---

## Parallel Example: Foundational Phase

```bash
# Após o Setup (Phase 1), lançar em paralelo:
Task: "Adicionar construtor privado de reconstrução a RecurringExpense em backend/Domain/Aggregates/RecurringExpense.cs"
Task: "Adicionar construtor privado de reconstrução a Occurrence em backend/Domain/Entities/Occurrence.cs"
Task: "Criar ContasEmDiaDbContext em backend/Infrastructure/Contexts/ContasEmDiaDbContext.cs"

# Em seguida (após os construtores existirem):
Task: "Criar RecurringExpenseConfigurations em backend/Infrastructure/Configs/RecurringExpenseConfigurations.cs"
Task: "Criar OccurrenceConfigurations em backend/Infrastructure/Configs/OccurrenceConfigurations.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar Phase 1: Setup
2. Completar Phase 2: Foundational (CRITICAL — bloqueia todas as stories)
3. Completar Phase 3: User Story 1 (Salvar despesa + ocorrência, com rollback atômico)
4. **STOP and VALIDATE**: Rodar `dotnet test Infrastructure.Tests` e confirmar as 3 Acceptance Scenarios de US1
5. Esse é o MVP: nenhuma despesa cadastrada pela tela "Nova despesa recorrente" se perde mais

### Incremental Delivery

1. Setup + Foundational → base pronta (schema criado, contrato compilável)
2. US1 → testar independentemente → MVP (persistência de escrita funcionando)
3. US2 → testar independentemente → contrato de leitura por id fechado
4. US3 → testar independentemente → contrato de listagem de ativas fechado
5. Polish → build limpo, validação manual de reconstrução, sem segredos versionados

---

## Notes

- [P] tasks = arquivos diferentes, sem dependências
- [Story] label mapeia a task para a user story correspondente (rastreabilidade)
- As três user stories compartilham o mesmo arquivo de implementação (`RecurringExpenseRepository.cs`, iniciado como stub em T009) e o mesmo arquivo de testes (`RecurringExpenseRepositoryTests.cs`) — por isso não são [P] entre si, mas continuam independentemente testáveis
- Verificar que os testes falham antes de implementar (T013-T015, T017-T018, T020-T021 antes de T016, T019, T022 respectivamente)
- Nenhuma migration de outra feature é alterada (não há nenhuma anterior — FR-013)
- Nenhuma regra de negócio é introduzida na Infrastructure (FR-010) — apenas tradução para operações EF Core
