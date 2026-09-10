# Implementation Plan: Editar Despesa Recorrente (Backend)

**Branch**: `007-edit-recurring-expense` | **Date**: 2026-09-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/007-edit-recurring-expense/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Adicionar a capacidade de editar uma despesa recorrente já cadastrada,
cobrindo as quatro camadas do backend descritas no refinamento técnico
([`refinements/backend/editar-despesa-recorrente.md`](../../refinements/backend/editar-despesa-recorrente.md)):
oito novos métodos de negócio no aggregate `RecurringExpense` (um por campo
editável, mais `Pause`/`Reactivate`), dois novos UseCases
(`GetRecurringExpenseById`/`UpdateRecurringExpense`), nenhuma mudança em
`Infrastructure`, e dois novos endpoints (`GET`/`PUT`) no
`RecurringExpensesController` já existente. Reaproveita integralmente as
validações de campo já fechadas no cadastro (Value Objects) e o mecanismo já
estabelecido de "não encontrado" via `KeyNotFoundException` capturada pelo
middleware global. As duas decisões que o refinamento e o refinamento
funcional deixaram como "a confirmar" — reativação gera automaticamente a
ocorrência da competência atual, e não há restrição adicional ao editar a
data de início — foram confirmadas via `/speckit-clarify` e ficam fechadas
para esta implementação (ver `spec.md`, seção "Clarifications").

## Technical Context

**Language/Version**: C# / .NET 10 (mesmo target já usado por Domain,
Application, Infrastructure e Api)

**Primary Dependencies**: Nenhuma dependência NuGet nova. Reaproveita
integralmente `ASP.NET Core Web API` e `Swashbuckle.AspNetCore` (já
referenciados por `backend/Api` desde a feature 004) e
`Microsoft.EntityFrameworkCore.SqlServer` 10 (já referenciado por
Infrastructure)

**Storage**: SQL Server via EF Core 10 (`ContasEmDiaDbContext`, já
implementado), sem nenhuma migration nova — os campos editáveis já são
colunas existentes de `RecurringExpenses` (ver `research.md` §5); nenhuma
mudança de schema é necessária

**Testing**: xUnit, estendendo os projetos de teste já existentes —
`Domain.Tests` (novos métodos do aggregate), `Application.Tests` (dois
novos UseCases) e `Api.Tests` (`WebApplicationFactory`, dois novos endpoints
no `RecurringExpensesControllerTests.cs` já existente) — nenhum projeto de
teste novo

**Target Platform**: ASP.NET Core (Kestrel), ambiente de desenvolvimento
confiável — sem autenticação/autorização/CORS nesta etapa (exceção vigente
do Princípio IV, já aplicada aos demais endpoints)

**Project Type**: web — extensão dos projetos .NET já existentes na
solution (`backend/ContasEmDia.sln`); nenhum projeto novo é criado por esta
feature

**Performance Goals**: Não especificado pela spec — nenhuma meta de
performance declarada; dois endpoints adicionais de baixo volume sobre uma
única linha (`RecurringExpense` por id)

**Constraints**: Toda resposta (sucesso/erro) dentro do envelope único já
existente `ApiResponse<TData>`; mensagens de erro de forma/presença e de
negócio em PT-BR; rotas sob `/api/v1/recurring-expenses/{id}`; nenhuma
lógica de negócio na API layer ou na Application layer (Princípios VI/XI/
XII); nenhuma regra de validação nova ou mais permissiva só por se tratar de
edição (FR-002); nenhuma ocorrência já gerada é reescrita (FR-003/FR-004)

**Scale/Scope**: Dois novos endpoints HTTP (`GET`/`PUT` por id) sobre um
recurso já existente, dois novos UseCases, oito novos métodos de negócio no
aggregate `RecurringExpense` — nenhuma mudança em Infrastructure, nenhuma
mudança em `OccurrencesController` ou nos UseCases de pagamento

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Avaliação |
|---|---|
| I — API-First Backend/Frontend Separation | PASS. Os dois novos endpoints são expostos exclusivamente via HTTP versionado; nenhum acesso direto a backend/BD é introduzido. Nenhum código de frontend é alterado por esta feature (ver `spec.md`, Assumptions). |
| II — Test-First Development | PASS (a verificar na implementação). Um teste por cenário de resposta declarado em cada camada: `Domain.Tests` (cada método novo do aggregate + `Reactivate` com/sem geração de ocorrência), `Application.Tests` (sucesso/não encontrado/falha de validação para cada UseCase), `Api.Tests` via `WebApplicationFactory` (`200`/`404`/`500` no `GET`, `200`/`400`/`404`/`500` no `PUT`). Nenhuma estrutura de pastas nova é introduzida — os testes entram nos arquivos/pastas já existentes por camada. |
| III — Type Safety & Static Analysis | PASS. Nenhuma mudança de configuração de projeto; `Nullable`/`TreatWarningsAsErrors` já habilitados em todos os projetos afetados. |
| IV — Secure Handling of Financial Data | PASS sob a exceção de fase vigente. Nenhuma auth/CORS implementada nos novos endpoints (mesmo débito explícito já registrado para os endpoints existentes). |
| V — Simplicity & Incremental Delivery | PASS. Um método de negócio por campo (sem introduzir um `Update(...)` genérico que expandiria a superfície pública sem necessidade — ver `research.md` §1); lógica de geração de ocorrência extraída para um único método privado compartilhado entre construtor e `Reactivate`, evitando duplicação sem introduzir abstração especulativa; nenhum projeto novo, nenhuma dependência nova. |
| VI — Domain-Driven Design in the Domain Layer | PASS — é o núcleo desta feature. Cada campo editável ganha um método de negócio dedicado (nenhum setter público é introduzido); cada método substitui a referência interna por um novo Value Object já validado pelo próprio construtor (sem duplicar validação); toda exceção de regra de negócio (se houver) carrega mensagem PT-BR. |
| VII — Infrastructure Layer | PASS, com uma correção pontual não prevista neste plano original. `IRecurringExpenseRepository.UpdateAsync`/`GetByIdAsync` já existiam e continuam suficientes na assinatura — nenhum método novo foi adicionado ao repositório — mas a validação manual contra PostgreSQL real (tasks.md, T041) encontrou dois defeitos pré-existentes, invisíveis a qualquer duplo de teste já usado no repositório: (1) a conversão EF Core de `_note` é ignorada pelo provider para uma coluna `NULL`, corrigido no construtor de reidratação do Domain (`note ?? new Note(null)`), não na Infrastructure; (2) `RecurringExpenseRepository.UpdateAsync` deixava a detecção automática de mudanças do EF Core decidir o estado de uma `Occurrence` nova adicionada a um agregado já rastreado (caso de `Reactivate`), que o EF classificava erroneamente como `UPDATE` em vez de `INSERT` por ter um Guid já não-padrão — corrigido marcando explicitamente `EntityState.Added` para ocorrências ainda `Detached` antes de `SaveChangesAsync()`. Ver `tasks.md` T041 e `research.md` §5 para o detalhamento completo. |
| XI — Application Layer | PASS. Dois novos UseCases, cada um em sua própria pasta, com `Input`/`Output` próprios (`GetRecurringExpenseByIdUseCaseOutput`/`UpdateRecurringExpenseUseCaseOutput` não são compartilhados entre si, apenas reaproveitam um DTO de dados aninhado — `RecurringExpenseData` — e o já existente `FieldError`, no mesmo padrão já usado por `PanelOccurrenceData`/`FieldError` entre `GetMonthlyPanel`/`MarkOccurrenceAsPaid`/`UndoOccurrencePayment`). Nenhuma regra de negócio é implementada nos UseCases — apenas orquestração (buscar, construir VOs, invocar métodos do aggregate, salvar). |
| XII — API Layer Implementation | PASS. Dois novos `DataRequest`/`DataResponse` (`UpdateRecurringExpenseDataRequest`/`RecurringExpenseDataResponse`, este último compartilhado entre `GET` e `PUT` por terem exatamente a mesma forma de sucesso), cada um com seu mapeamento dedicado; `ProducesResponseType` completo em cada ação; envelope único reaproveitado; `[Required]` com `ErrorMessage` PT-BR no `PUT`; nenhuma lógica de negócio no controller; middleware global de exceções já trata `KeyNotFoundException` → `404`, sem necessidade de alteração. |

Nenhuma violação identificada. `Complexity Tracking` não é necessário.

## Project Structure

### Documentation (this feature)

```text
specs/007-edit-recurring-expense/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/
│   └── api-contract.md  # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── Domain/
│   └── Aggregates/
│       └── RecurringExpense.cs              # MODIFICADO — 8 novos métodos de negócio + método privado
│                                             # compartilhado de geração de ocorrência (ver data-model.md) +
│                                             # normalização note ?? new Note(null) no construtor de
│                                             # reidratação do EF (fix descoberto em T041, research.md §5)
├── Domain.Tests/
│   └── Aggregates/
│       └── RecurringExpenseTests.cs         # MODIFICADO — novos testes para cada método novo
├── Application/
│   ├── UseCases/
│   │   ├── CreateRecurringExpense/          # já existe — não modificado
│   │   ├── GetRecurringExpenseById/         # NOVO
│   │   │   ├── IGetRecurringExpenseByIdUseCase.cs
│   │   │   ├── GetRecurringExpenseByIdUseCase.cs
│   │   │   ├── GetRecurringExpenseByIdUseCaseInput.cs
│   │   │   └── GetRecurringExpenseByIdUseCaseOutput.cs   # define RecurringExpenseData (record compartilhado)
│   │   └── UpdateRecurringExpense/          # NOVO
│   │       ├── IUpdateRecurringExpenseUseCase.cs
│   │       ├── UpdateRecurringExpenseUseCase.cs
│   │       ├── UpdateRecurringExpenseUseCaseInput.cs
│   │       └── UpdateRecurringExpenseUseCaseOutput.cs    # reaproveita RecurringExpenseData + FieldError
│   └── Ports/                               # já existe — não modificado
├── Application.Tests/
│   └── UseCases/
│       ├── GetRecurringExpenseById/         # NOVO
│       └── UpdateRecurringExpense/          # NOVO
├── Infrastructure/
│   ├── Configs/RecurringExpenseConfigurations.cs  # não modificado (o fix real ficou no Domain — ver research.md §5)
│   └── Repositories/RecurringExpenseRepository.cs # MODIFICADO — UpdateAsync marca ocorrências novas como Added (research.md §5)
├── Infrastructure.Tests/                    # MODIFICADO — 2 testes de regressão novos em RecurringExpenseRepositoryTests.cs (research.md §5)
├── Api/
│   ├── Controllers/
│   │   └── RecurringExpensesController.cs   # MODIFICADO — 2 novas ações (GetById, Put)
│   ├── Requests/
│   │   └── UpdateRecurringExpenseDataRequest.cs           # NOVO
│   ├── Responses/
│   │   └── RecurringExpenseDataResponse.cs                # NOVO — compartilhado por GET e PUT
│   └── Mappings/
│       ├── UpdateRecurringExpenseDataRequestMapping.cs    # NOVO
│       └── RecurringExpenseDataResponseMapping.cs         # NOVO — a partir de RecurringExpenseData
├── Api.Tests/
│   └── Controllers/
│       └── RecurringExpensesControllerTests.cs            # MODIFICADO — novos testes para GET e PUT
└── ContasEmDia.sln                          # não modificada — nenhum projeto novo

frontend/                                    # já existe — nenhum código de frontend alterado por esta feature
```

**Structure Decision**: Nenhum projeto novo é adicionado à solution. Esta
feature estende os quatro projetos backend já existentes
(`Domain`, `Application`, `Api`, e seus respectivos `.Tests`), seguindo
exatamente as mesmas convenções de pastas e nomenclatura já usadas pelas
features 001–006 (`/Aggregates`, `/UseCases/<Nome>/`, `/Controllers`,
`/Requests`, `/Responses`, `/Mappings`). `Infrastructure` permanece
inalterado (ver `research.md` §5).

## Complexity Tracking

*Nenhuma violação da Constitution Check acima — seção não aplicável.*
