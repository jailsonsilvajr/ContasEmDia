# Implementation Plan: Application de Despesa Recorrente (Cadastro)

**Branch**: `003-despesa-recorrente-application` | **Date**: 2026-09-04 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-despesa-recorrente-application/spec.md`

## Summary

Criar o projeto `backend/Application` (`ContasEmDia.Application`), contendo o único Use Case desta etapa — `CreateRecurringExpenseUseCase` — que recebe dados primitivos equivalentes ao corpo do `POST /api/recurring-expenses`, converte cada campo para o Value Object do Domain correspondente (agregando todos os erros de campo em vez de parar no primeiro), obtém a data atual através de um novo port `ICurrentDateProvider` (declarado nesta feature, implementado por uma feature futura de Infrastructure), deriva a competência corrente, cria `RecurringExpense` exclusivamente pelo construtor público, persiste via uma nova interface `IRepositoryManager` (declarada no Domain, implementada pelo `RepositoryManager` já existente na Infrastructure) e monta um Output de sucesso ou de falha de validação — sem nenhum tipo de transporte HTTP envolvido. Como pré-requisitos estruturais desta feature (decididos na clarificação da spec): (1) extrair `IRepositoryManager` no Domain e fazer `RepositoryManager` implementá-la; (2) revisar para PT-BR as mensagens de erro hoje em inglês nos Value Objects/aggregate do Domain, sem alterar nenhuma regra de negócio.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`net10.0`), consistente com `backend/Domain` e `backend/Infrastructure`.

**Primary Dependencies**: Nenhuma dependência externa nova no projeto `Application` (apenas BCL — sem ASP.NET Core, sem EF Core, sem bibliotecas de validação); `Application.Tests` usa `xUnit` + `Microsoft.NET.Test.Sdk` + `coverlet.collector` (mesmo padrão de `Domain.Tests`), com dublês de teste escritos à mão (sem biblioteca de mocking — nenhuma é usada em nenhum lugar do repositório hoje).

**Storage**: N/A diretamente — o Use Case depende apenas da abstração `IRecurringExpenseRepository` (Domain) já implementada pela Infrastructure existente; nenhum código de persistência é adicionado ou alterado por esta feature.

**Testing**: `xUnit` no novo projeto `Application.Tests`, seguindo o padrão de `Domain.Tests`/`Infrastructure.Tests`: dublês de teste para `IRepositoryManager`/`IRecurringExpenseRepository` (reaproveitando o padrão de `InMemoryRecurringExpenseRepository` de `Domain.Tests`) e para `ICurrentDateProvider` (data fixa controlável pelo teste), cobrindo as 3 User Stories e os edge cases de FR-001–FR-016.

**Target Platform**: Servidor backend .NET (biblioteca de classes consumida futuramente por uma camada de API ainda não criada; não é uma aplicação executável por si só).

**Project Type**: Biblioteca backend (camada de orquestração) dentro da solução `backend/ContasEmDia.sln`, adicionando os projetos `Application` e `Application.Tests` ao lado dos já existentes `Domain`/`Domain.Tests`/`Infrastructure`/`Infrastructure.Tests`.

**Performance Goals**: Não especificado no refinamento de origem nem na spec; nenhuma meta quantitativa de performance definida para esta feature — N/A.

**Constraints**: Sem nenhum tipo de transporte HTTP (FR-013); Use Case não implementa regra de negócio própria (FR-014); criação do aggregate exclusivamente pelo construtor público (FR-008); persistência exclusivamente via `IRepositoryManager`/`IRecurringExpenseRepository` do Domain, nunca via classe concreta da Infrastructure (FR-009, FR-015); falhas não relacionadas a validação de negócio devem propagar como exceção, nunca virar erro de campo (FR-012).

**Scale/Scope**: Um único Use Case (`CreateRecurringExpenseUseCase`); um novo port (`ICurrentDateProvider`); uma nova interface de acesso a repositórios (`IRepositoryManager`, no Domain); revisão textual (não estrutural) das mensagens de erro de 7 Value Objects e potencialmente do aggregate no Domain. Nenhum controller, rota ou projeto de API nesta etapa.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplicação a esta feature | Status |
|---|---|---|
| I. API-First Backend/Frontend Separation | Fora de escopo nesta feature (sem controllers/endpoints); o Use Case não introduz nenhum acesso direto do frontend ao backend. | PASS |
| II. Test-First Development | `Application.Tests` (xUnit) deve ser escrito antes/junto da implementação do Use Case, cobrindo as 3 User Stories e os edge cases de FR-001–FR-016, com dublês para `IRepositoryManager` e `ICurrentDateProvider`. | PASS (a aplicar durante `/speckit-tasks` e `/speckit-implement`) |
| III. Type Safety & Static Analysis | Novo projeto `Application` usa `Nullable enable` e `TreatWarningsAsErrors true`, mesmo padrão de `Domain.csproj`/`Infrastructure.csproj`. | PASS |
| IV. Secure Handling of Financial Data | Sem autenticação/autorização/endpoints nesta etapa (fora de escopo — camada de API futura); nenhum dado sensível é logado ou exposto por este Use Case. | PASS |
| V. Simplicity & Incremental Delivery | Única abstração nova de infraestrutura é `ICurrentDateProvider` (já justificada pelo refinamento de origem); `IRepositoryManager` reaproveita a interface já decidida na clarificação da spec; nenhuma abstração genérica de Use Case compartilhada é criada (não há ainda um segundo Use Case que a justifique). | PASS |
| VI. Domain-Driven Design in the Domain Layer | Esta feature toca o Domain em dois pontos estruturais: (a) adiciona `IRepositoryManager` ao Domain (FR-015); (b) revisa mensagens de erro para PT-BR nos Value Objects/aggregate (FR-016) — nenhuma das duas altera regra de negócio, apenas texto de mensagem e uma nova interface de leitura. Nenhuma regra de validação é reimplementada na Application (FR-002, FR-014). | GATE — resolvido pelo escopo desta feature (ver Complexity Tracking), mesmo padrão já usado por `002-despesa-recorrente-infrastructure` |
| VII. Infrastructure Layer Implementation | `RepositoryManager` já existente passa a implementar a nova `IRepositoryManager` do Domain (mudança de assinatura, não de estrutura); nenhuma outra alteração de Infrastructure nesta feature. | PASS |
| VIII–X (Angular/Frontend) | Não aplicável — esta feature é exclusivamente backend. | N/A |
| XI. Application Layer Implementation | O novo projeto segue a estrutura `/UseCases/CreateRecurringExpense/` com `ICreateRecurringExpenseUseCase`/`CreateRecurringExpenseUseCase` e `CreateRecurringExpenseUseCaseInput`/`CreateRecurringExpenseUseCaseOutput`; o Use Case apenas orquestra (converte entrada, invoca o construtor do aggregate, chama `SaveChangesAsync` via repositório); o projeto `Application` referencia exclusivamente `Domain` no `.csproj`. | PASS |

Nenhuma violação não justificada. Uma tensão textual entre a Assumption da spec (o port `ICurrentDateProvider` é declarado nesta camada — Application) e a redação literal do Princípio XI ("Infrastructure implementations MUST be supplied to Application through Domain-defined abstractions") é discutida em `research.md` §2 e não bloqueia esta feature: nenhuma implementação de Infrastructure é conectada a este port nesta etapa (a spec Assumptions explicitamente exclui a implementação concreta do escopo), então nenhuma "supply" de fato ocorre ainda para violar o gate — a decisão de onde a implementação futura será conectada fica registrada como ponto a revisitar quando essa implementação for criada.

**Re-check pós-Phase 1**: `data-model.md` e `contracts/application-public-api.md` confirmam que o Use Case depende apenas de tipos do próprio `Application` e do `Domain` (nenhuma referência a `Infrastructure`), que toda validação de campo é apenas tradução de exceções já lançadas pelo Domain (nenhuma regra nova), e que `IRepositoryManager`/`ICurrentDateProvider` são as únicas abstrações novas. Nenhuma nova violação introduzida pelo design; gate permanece PASS.

## Project Structure

### Documentation (this feature)

```text
specs/003-despesa-recorrente-application/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md         # Phase 1 output (/speckit-plan command)
├── contracts/            # Phase 1 output (/speckit-plan command)
│   └── application-public-api.md
└── tasks.md              # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── ContasEmDia.sln
├── Domain/                                          # already exists
│   ├── Aggregates/RecurringExpense.cs                 # MODIFIED: PT-BR error messages only (FR-016)
│   ├── Entities/Occurrence.cs                         # unaffected (no field-error messages of its own)
│   ├── ValueObjects/*.cs                              # MODIFIED: PT-BR error messages only (FR-016)
│   └── Repositories/
│       ├── IRecurringExpenseRepository.cs             # unaffected
│       └── IRepositoryManager.cs                      # NEW: extracted per clarification (FR-015)
├── Domain.Tests/                                      # already exists; message-text assertions updated for FR-016
├── Infrastructure/                                    # already exists
│   └── RepositoryManager.cs                           # MODIFIED: implements Domain.Repositories.IRepositoryManager
├── Infrastructure.Tests/                              # already exists, unaffected
├── Application/                                       # NEW PROJECT
│   ├── ContasEmDia.Application.csproj
│   ├── Ports/
│   │   └── ICurrentDateProvider.cs
│   └── UseCases/
│       └── CreateRecurringExpense/
│           ├── ICreateRecurringExpenseUseCase.cs
│           ├── CreateRecurringExpenseUseCase.cs
│           ├── CreateRecurringExpenseUseCaseInput.cs
│           └── CreateRecurringExpenseUseCaseOutput.cs
└── Application.Tests/                                  # NEW PROJECT
    ├── ContasEmDia.Application.Tests.csproj
    └── UseCases/
        └── CreateRecurringExpense/
            ├── CreateRecurringExpenseUseCaseTests.cs
            ├── FakeRepositoryManager.cs
            ├── InMemoryRecurringExpenseRepository.cs   # or shared/duplicated from Domain.Tests pattern
            ├── FixedCurrentDateProvider.cs
            └── ThrowingRecurringExpenseRepository.cs
```

**Structure Decision**: Solução backend existente (`backend/ContasEmDia.sln`) ganha dois novos projetos irmãos de `Domain`/`Infrastructure`: `Application` (produção, referenciando exclusivamente `ContasEmDia.Domain`) e `Application.Tests` (xUnit, referenciando `ContasEmDia.Application`). Segue o padrão de nomenclatura e de `TargetFramework`/`Nullable`/`TreatWarningsAsErrors` já estabelecido por `Domain.csproj`/`Infrastructure.csproj`. Não há projeto de API/host nesta feature — a composição via injeção de dependência (registrar `ICurrentDateProvider`, `IRepositoryManager`, o Use Case) fica para a futura camada de API. `IRepositoryManager` é colocada em `Domain/Repositories/` junto de `IRecurringExpenseRepository.cs`, por ser o local natural já convencionado para contratos de acesso a repositório neste projeto, ainda que não seja em si um repositório de um Aggregate específico.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Modificação do Domain (nova interface `IRepositoryManager`) dentro de uma feature de Application | A clarificação da spec decidiu que o Use Case deve depender de uma interface (não da classe concreta `RepositoryManager` da Infrastructure) para obter o repositório, preservando a inversão de dependência exigida pelo Princípio XI ("Application MUST NOT reference Infrastructure"); sem essa interface no Domain, o Use Case teria que referenciar `Infrastructure` diretamente. | Manter a dependência direta na classe concreta `RepositoryManager` foi rejeitado explicitamente na clarificação da spec, por violar a direção de dependência exigida pelo Princípio XI e pela Clean Architecture já adotada no projeto. |
| Modificação do Domain (mensagens de erro em PT-BR) dentro de uma feature de Application | A clarificação da spec decidiu que essa revisão textual é pré-requisito de FR-006/SC-003 (o Use Case só repassa a mensagem do Domain, nunca mantém texto duplicado) e deve ser feita como parte deste trabalho, seguindo o mesmo padrão já usado por `002-despesa-recorrente-infrastructure` (que também ajustou o Domain como pré-requisito estrutural). | Manter as mensagens em inglês e traduzi-las/duplicá-las na Application foi rejeitado explicitamente na clarificação da spec e pelo Princípio VI ("Outer layers... MUST NOT maintain a separate, duplicated set of messages"). |
