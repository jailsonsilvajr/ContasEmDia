# Implementation Plan: Provider de Banco de Dados PostgreSQL

**Branch**: `005-postgresql-provider` | **Date**: 2026-09-06 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/005-postgresql-provider/spec.md`

## Summary

Substituir SQL Server por PostgreSQL como único provider de persistência da
aplicação, sem alterar nenhum comportamento observável pela API ou pelas
telas já existentes. A abordagem técnica é trocar o provider EF Core
(`Microsoft.EntityFrameworkCore.SqlServer` → `Npgsql.EntityFrameworkCore.
PostgreSQL`) e o provider de testes de infraestrutura
(`Testcontainers.MsSql` → `Testcontainers.PostgreSql`), normalizar o único
mapeamento de coluna específico de T-SQL (`decimal(18,2)`) para a API
agnóstica de provider do EF Core (`HasPrecision`), regerar a migração
inicial do schema para o novo provider, atualizar as connection strings de
cada ambiente, e disponibilizar um `docker-compose.yml` para subir
PostgreSQL localmente sem dependência de SQL Server.

## Technical Context

**Language/Version**: C# / .NET 10 (backend `Infrastructure`, `Api` e seus
projetos de teste — únicos projetos afetados; Domain e Application não têm
dependência de persistência, conforme Principle XI).

**Primary Dependencies**: `Npgsql.EntityFrameworkCore.PostgreSQL` (substitui
`Microsoft.EntityFrameworkCore.SqlServer`) e `Testcontainers.PostgreSql`
(substitui `Testcontainers.MsSql`), ambos na versão major mais recente
compatível com EF Core 10 e com o Testcontainers 4.x já em uso.

**Storage**: PostgreSQL (versão estável mais recente disponível no momento
da implementação — ver [research.md](./research.md)), substituindo SQL
Server como único banco relacional suportado.

**Testing**: xUnit (já em uso) com Testcontainers.PostgreSql para os testes
reais de repositório em `Infrastructure.Tests`; `Api.Tests` continua usando
EF Core InMemory (não afetado); `Domain.Tests`/`Application.Tests` não têm
dependência de banco de dados.

**Target Platform**: Backend ASP.NET Core (.NET 10), Linux/Windows — sem
mudança de plataforma de execução, apenas de banco de dados.

**Project Type**: Web application (backend .NET + frontend Angular já
existentes) — esta feature é restrita ao backend, camada de persistência.

**Performance Goals**: N/A — a spec não define nenhuma meta de performance
nova; o requisito é paridade de comportamento observável com o SQL Server
atual (FR-003), não melhoria de performance.

**Constraints**: Nenhuma dependência de execução remanescente de SQL Server
após a troca (FR-007); sem migração de dados de produção, pois não existem
dados reais persistidos até o momento (ver spec, Assumptions).

**Scale/Scope**: Escopo restrito às entidades de domínio já existentes
(`RecurringExpense`, `Occurrence`) e à camada `Infrastructure` + composição
em `Api/Program.cs`/`appsettings*`; nenhuma nova entidade, endpoint ou
funcionalidade de negócio é introduzida.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Principle VII (Infrastructure Layer Implementation)** — ✅ Compatível.
  A troca continua implementando todas as interfaces de repositório do
  Domain, mantém a estrutura de pastas exigida
  (`/Repositories`, `/Migrations`, `/Configs`, `/Contexts`), continua usando
  EF Core 10, não introduz Unit of Work customizado, e mantém o acesso via
  `RepositoryManager` com propriedades `Lazy<T>` — nenhuma dessas
  implementações muda, apenas o provider configurado no `DbContextOptions`.
  ⚠️ **Exceção pontual e justificada**: a regra "EF Core migrations
  belonging to previously delivered features MUST NOT be altered, renamed,
  or deleted" seria violada literalmente ao remover a migração
  `20260831201615_InitialCreate`. Esta é a única exceção do plano — ver
  seção Complexity Tracking abaixo para a justificativa completa.
- **Principle V (Simplicity & Incremental Delivery)** — ✅ Compatível. Não
  é introduzida nenhuma abstração nova de acesso a dados; a troca reutiliza
  o mesmo modelo EF Core já adotado, apenas trocando o provider e
  normalizando uma string de tipo específica de SQL Server para uma API
  agnóstica de provider (`HasPrecision`).
- **Principle VI (DDD no Domain layer)** — ✅ Sem impacto. Nenhum Aggregate,
  Entity ou Value Object é criado, removido ou alterado.
- **Principle XI (Application Layer)** — ✅ Sem impacto. Nenhuma UseCase é
  criada, removida ou alterada; Application continua dependendo apenas do
  Domain.
- **Principle XII (API Layer)** — ✅ Sem impacto observável. Nenhum
  endpoint, `DataRequest`/`DataResponse`, envelope de resposta ou
  middleware é alterado; `Api.Tests` usa EF Core InMemory e não depende do
  provider real, então a suíte de testes de contrato da API permanece
  válida sem mudanças.
- **Principle II (Test-First Development)** — ✅ Compatível. Os testes de
  repositório existentes em `Infrastructure.Tests` continuam sendo a fonte
  de verdade; eles são portados (mesmos casos, fixture trocada) antes/junto
  da troca de provider, preservando a garantia de que a suíte falha se o
  comportamento observável divergir.
- **AI Agent Guardrails** — ✅ Compatível. `Npgsql.EntityFrameworkCore.
  PostgreSQL` e `Testcontainers.PostgreSql` são dependências .NET (NuGet),
  não npm — a regra de plano+validação humana antes de dependência nova se
  aplica apenas a dependências npm do frontend; ainda assim, ambas as
  dependências novas já estão nomeadas e justificadas neste plano
  (Technical Context) para transparência.

**Resultado do gate**: PASSA, com uma exceção justificada e documentada
(ver Complexity Tracking).

## Project Structure

### Documentation (this feature)

```text
specs/005-postgresql-provider/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

Não há pasta `contracts/`: esta feature não expõe nenhuma interface nova
nem altera o contrato de API já documentado em
`specs/004-api-despesa-recorrente/contracts/` — o requisito central (FR-003)
é justamente que esse contrato permaneça idêntico.

### Source Code (repository root)

```text
backend/
├── Domain/                          # inalterado
├── Application/                     # inalterado
├── Infrastructure/
│   ├── ContasEmDia.Infrastructure.csproj   # pacote SqlServer → Npgsql
│   ├── Contexts/
│   │   ├── ContasEmDiaDbContext.cs         # inalterado
│   │   └── ContasEmDiaDbContextFactory.cs  # UseSqlServer → UseNpgsql
│   ├── Configs/
│   │   ├── RecurringExpenseConfigurations.cs   # decimal(18,2) → HasPrecision(18,2)
│   │   └── OccurrenceConfigurations.cs         # decimal(18,2) → HasPrecision(18,2)
│   └── Migrations/
│       └── <nova migração inicial para Npgsql>  # substitui a migração SQL Server
├── Infrastructure.Tests/
│   ├── ContasEmDia.Infrastructure.Tests.csproj  # Testcontainers.MsSql → Testcontainers.PostgreSql
│   ├── PostgreSqlContainerFixture.cs             # renomeia/substitui SqlServerContainerFixture.cs
│   └── Repositories/
│       └── RecurringExpenseRepositoryTests.cs    # troca de [Collection] para PostgreSqlCollection
├── Api/
│   ├── Program.cs                    # UseSqlServer → UseNpgsql
│   ├── appsettings.json              # sem mudança de estrutura (placeholder vazio)
│   └── appsettings.Development.json  # connection string Npgsql
└── Api.Tests/                        # inalterado (EF Core InMemory)

docker-compose.yml                    # novo — sobe PostgreSQL local (User Story 2)
```

**Structure Decision**: Estrutura de projeto web (backend .NET + frontend
Angular) já estabelecida é mantida sem alteração. Todas as mudanças ficam
contidas nos diretórios `backend/Infrastructure`, `backend/Infrastructure.
Tests` e `backend/Api` já existentes, mais um `docker-compose.yml` novo na
raiz do repositório para suporte a desenvolvimento local (User Story 2).
Nenhum projeto novo é criado.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|---------------------------------------|
| Remover/substituir a migração `20260831201615_InitialCreate` (Principle VII: "migrations belonging to previously delivered features MUST NOT be altered, renamed, or deleted") | O corpo de uma migração EF Core (`Up`/`Down`) é gerado em T-SQL específico do provider SQL Server e não pode ser executado contra PostgreSQL — não existe forma de "portar" seu conteúdo, apenas regerá-lo para o novo provider. O próprio spec (seção Edge Cases) já antecipa e autoriza explicitamente reiniciar o histórico de migração do zero para o novo provider, justamente porque não há dados de produção a preservar (spec, Assumptions). | Manter a migração SQL Server antiga inerte e adicionar uma segunda migração inicial só para PostgreSQL foi rejeitado: geraria duas migrações "iniciais" incompatíveis coexistindo no histórico do EF Core sem nenhum benefício, já que nenhum ambiente real rodou a migração antiga (projeto ainda não está em produção). |

## Extension Hooks

Nenhum hook `before_plan`/`after_plan` configurado em `.specify/extensions.yml`
(arquivo não encontrado no repositório) — etapa de hooks ignorada
silenciosamente, conforme regra do comando.
