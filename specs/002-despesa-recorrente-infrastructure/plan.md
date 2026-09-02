# Implementation Plan: Infrastructure de Despesa Recorrente (Persistência)

**Branch**: `002-despesa-recorrente-infrastructure` | **Date**: 2026-08-31 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-despesa-recorrente-infrastructure/spec.md`

## Summary

Criar o projeto `backend/Infrastructure` (`ContasEmDia.Infrastructure`), implementando com EF Core 10 sobre SQL Server a persistência do aggregate `RecurringExpense` e da entidade `Occurrence` já existentes no Domain: um `DbContext` (`ContasEmDiaDbContext`), configurações de mapeamento (`IEntityTypeConfiguration<T>`) para o aggregate, a entidade e cada um dos seus Value Objects, uma migration inicial, a implementação concreta de `IRecurringExpenseRepository`, e um `RepositoryManager` que expõe o repositório via `Lazy<T>`. Como pré-requisito estrutural (Princípio VI da constituição), esta feature também adiciona ao Domain o construtor privado de reconstrução em `RecurringExpense` e `Occurrence`, sem alterar nenhuma regra de negócio. Os testes de persistência (incluindo a comprovação de rollback atômico) rodam contra SQL Server real via Testcontainers.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`net10.0`), consistente com `backend/Domain`

**Primary Dependencies**: `Microsoft.EntityFrameworkCore.SqlServer` 10.x, `Microsoft.EntityFrameworkCore.Design` 10.x (para migrations); testes de integração usam `Testcontainers.MsSql` + `xunit` + `Microsoft.NET.Test.Sdk` (mesmo padrão de `Domain.Tests`)

**Storage**: SQL Server (via EF Core 10), relação 1:N explícita (FK) entre `RecurringExpense` e `Occurrence` (não owned entity), conforme decisão já confirmada no refinamento de origem

**Testing**: xUnit para testes de integração de persistência (`Infrastructure.Tests`), rodando contra um container SQL Server descartável por execução via Testcontainers — sem provedor InMemory do EF Core

**Target Platform**: Servidor backend .NET (biblioteca de classes consumida futuramente por uma camada de API ainda não criada; não é uma aplicação executável por si só)

**Project Type**: Biblioteca backend (camada de persistência) dentro da solução `backend/ContasEmDia.sln`, adicionando um novo projeto `Infrastructure` ao lado do já existente `Domain`

**Performance Goals**: Não especificado no refinamento de origem; nenhuma meta quantitativa de performance definida para esta feature — N/A

**Constraints**: String de conexão e configurações sensíveis exclusivamente via configuração externa (nunca hardcoded); persistência da despesa + ocorrência do mês corrente em uma única chamada a `SaveChangesAsync` (sem Unit of Work customizado); nenhuma migration de outra feature pode ser alterada (não há nenhuma anterior); reconstrução via banco não pode reaplicar nem contornar validações de criação do Domain

**Scale/Scope**: Um único aggregate (`RecurringExpense`) e uma única entidade dependente (`Occurrence`); três operações de repositório (`AddAsync`, `GetByIdAsync`, `GetActiveAsync`); sem camada de API/controllers nesta etapa

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplicação a esta feature | Status |
|---|---|---|
| I. API-First Backend/Frontend Separation | Fora de escopo nesta feature (sem controllers/endpoints); não introduz acesso direto do frontend ao banco. | PASS |
| II. Test-First Development | Testes de integração de persistência (Testcontainers) devem ser escritos antes da implementação dos repositórios/DbContext, cobrindo as 3 User Stories e os edge cases de FR-001–FR-009. | PASS (a aplicar durante `/speckit-tasks` e `/speckit-implement`) |
| III. Type Safety & Static Analysis | Novo projeto `Infrastructure` usa `Nullable` habilitado e `TreatWarningsAsErrors`, mesmo padrão do `Domain.csproj`. | PASS |
| IV. Secure Handling of Financial Data | FR-011/SC-005: string de conexão via configuração externa (`appsettings`/variáveis de ambiente, não versionada); sem autenticação/autorização nesta etapa (fora de escopo, conforme Assumptions). | PASS |
| V. Simplicity & Incremental Delivery | Sem abstrações além do exigido (sem Unit of Work customizado, sem camadas extras); reconstrução via banco reaproveita construtores existentes onde possível. | PASS |
| VI. Domain-Driven Design in the Domain Layer | O Domain hoje **não** possui o construtor privado de reconstrução exigido por este princípio em `RecurringExpense` e `Occurrence` — violação pré-existente identificada durante a clarificação da spec. Esta feature resolve a violação adicionando esse construtor (FR-009), sem alterar regra de negócio. | GATE — resolvido pelo escopo desta feature (ver Complexity Tracking) |
| VII. Infrastructure Layer Implementation | Estrutura de pastas (`/Repositories`, `/Migrations`, `/Configs`, `/Contexts`), EF Core 10, sem Unit of Work customizado, `RepositoryManager` com propriedades `Lazy<T>` — todos exigidos explicitamente pelos FR-012/FR-013 e pelo refinamento de origem. | PASS |

Nenhuma violação não justificada. A única lacuna constitucional pré-existente (VI) é o próprio motivo de FR-009 e está documentada em Complexity Tracking.

**Re-check pós-Phase 1**: o design em `data-model.md` e `contracts/repository-contract.md` confirma que apenas `RecurringExpense` e `Occurrence` precisam do construtor privado de reconstrução (Value Objects reutilizam seus construtores públicos, ver `research.md` §1) e que o `RepositoryManager`/`ContasEmDiaDbContext` seguem exatamente a forma exigida pelo Princípio VII. Nenhuma nova violação introduzida pelo design; gate permanece PASS.

## Project Structure

### Documentation (this feature)

```text
specs/002-despesa-recorrente-infrastructure/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── ContasEmDia.sln
├── Domain/                                    # already exists
│   ├── Aggregates/RecurringExpense.cs          # MODIFIED: add private reconstruction ctor
│   ├── Entities/Occurrence.cs                  # MODIFIED: add private reconstruction ctor
│   ├── ValueObjects/*.cs                       # possibly modified if reconstruction requires it
│   └── Repositories/IRecurringExpenseRepository.cs
├── Domain.Tests/                               # already exists, unaffected
├── Infrastructure/                             # NEW PROJECT
│   ├── ContasEmDia.Infrastructure.csproj
│   ├── Contexts/
│   │   └── ContasEmDiaDbContext.cs
│   ├── Configs/
│   │   ├── RecurringExpenseConfigurations.cs
│   │   └── OccurrenceConfigurations.cs
│   ├── Migrations/
│   │   └── <timestamp>_InitialCreate.cs
│   ├── Repositories/
│   │   └── RecurringExpenseRepository.cs
│   └── RepositoryManager.cs
└── Infrastructure.Tests/                       # NEW PROJECT
    ├── ContasEmDia.Infrastructure.Tests.csproj
    └── Repositories/
        └── RecurringExpenseRepositoryTests.cs  # Testcontainers-backed integration tests
```

**Structure Decision**: Solução backend existente (`backend/ContasEmDia.sln`) ganha dois novos projetos irmãos de `Domain`/`Domain.Tests`: `Infrastructure` (produção) e `Infrastructure.Tests` (testes de integração via Testcontainers). Segue o padrão de nomenclatura e de `TargetFramework`/`Nullable`/`TreatWarningsAsErrors` já estabelecido pelo `Domain.csproj`. Não há projeto de frontend nem de API envolvido nesta feature.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Modificação do Domain (Princípio VI) dentro de uma feature de Infrastructure | O Domain hoje não expõe o construtor privado de reconstrução exigido pela constituição; sem ele, `Infrastructure` não consegue materializar instâncias a partir do banco sem reexecutar a lógica de decisão do construtor público (ex.: gerar ocorrência do mês corrente novamente) — violando FR-009/FR-010. | Implementar reconstrução via reflexão ou contornando o encapsulamento do Domain foi rejeitado por violar diretamente o Princípio VI (nenhuma criação fora dos construtores) e por acoplar Infrastructure a detalhes internos frágeis do Domain. A opção mais simples e já autorizada pela clarificação da spec é adicionar o construtor privado mínimo ao próprio Domain. |
