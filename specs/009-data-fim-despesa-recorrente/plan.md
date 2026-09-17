# Implementation Plan: Data de Fim da Despesa Recorrente

**Branch**: `009-data-fim-despesa-recorrente` | **Date**: 2026-09-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/009-data-fim-despesa-recorrente/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Adicionar um campo obrigatório "Data de fim" à despesa recorrente (vigência
máxima de 1 ano a partir da data de início), seguindo o refinamento técnico
([`refinements/data-fim-despesa-recorrente.md`](../../refinements/data-fim-despesa-recorrente.md)),
cobrindo as quatro camadas de backend e as duas telas de frontend já
implementadas: **Domain** — novo campo `_endDate` no aggregate
`RecurringExpense`, duas invariantes cruzadas (fim > início; vigência ≤ 1
ano) validadas no construtor e em `ChangeStartDate`/`ChangeEndDate` (novo
método), mais a invariante "fim não pode estar no passado" (FR-014,
validada só no caminho de `endDate`), e reformulação da geração de
ocorrências no cadastro para cobrir toda a vigência (mês atual até a
competência de fim, inclusive) **independentemente de a despesa ser Ativa
ou Pausada**; **Application** — `CreateRecurringExpenseUseCase` e
`UpdateRecurringExpenseUseCase` passam a capturar violações de regra de
negócio na fronteira Domain→Application e traduzi-las em `FieldError` por
campo (`endDate` ou `startDate`, conforme o método que falhou); **Api** —
`endDate` adicionado aos `DataRequest`/`DataResponse` já existentes de
`RecurringExpensesController` (`POST`/`GET`/`PUT`), sem endpoint novo;
**Infrastructure** — nova coluna `EndDate` (não anulável) via migração EF
Core com backfill (`EndDate = StartDate + 1 ano` para despesas já
cadastradas, User Story 4); **Frontend** — campo "Data de fim" replicado
nos formulários de cadastro (`design/Cadastro.dc.html`, já atualizado) e
edição, reaproveitando o padrão já usado por "Data de início" nos dois
componentes Angular existentes. Reaproveita integralmente o mecanismo de
erro por campo (`FieldError`), o envelope `ApiResponse<TData>`, e o padrão
de "não encontrado" via `KeyNotFoundException` já fixados pelas features
001–008; nenhum projeto novo, nenhuma rota nova, nenhuma dependência nova.

## Technical Context

**Language/Version**: C# / .NET 10 (backend — Domain, Application,
Infrastructure, Api) e TypeScript / Angular v22 em modo `strict` (frontend
— já usados por todas as features anteriores)

**Primary Dependencies**: Nenhuma dependência NuGet ou npm nova. Backend
reaproveita `ASP.NET Core Web API`, `Swashbuckle.AspNetCore` e
`Microsoft.EntityFrameworkCore.PgSQL`/Npgsql 10 (já referenciados desde as
features 004/005); frontend reaproveita Angular Signals, `HttpClient` via
`inject()`, Tailwind CSS e Angular Router (já usados por
`cadastro-despesa-recorrente`/`editar-despesa-recorrente`)

**Storage**: PostgreSQL via EF Core 10 (`ContasEmDiaDbContext`, já
implementado) — uma nova migração adiciona a coluna `EndDate` (`date`, não
anulável) à tabela `RecurringExpenses`, com backfill por linha
(`EndDate = StartDate + 1 ano`) para as despesas já cadastradas (ver
`research.md` §8); nenhuma mudança de schema em `Occurrences`

**Testing**: xUnit, estendendo os quatro projetos de teste de backend já
existentes (`Domain.Tests`, `Application.Tests`, `Api.Tests` via
`WebApplicationFactory`, `Infrastructure.Tests` — este último ganha um
teste de regressão para confirmar que remover uma `Occurrence` de um
aggregate rastreado gera `DELETE` real contra PostgreSQL, ver
`research.md` §6); Vitest/Jasmine (o que já estiver configurado) para os
component specs de `cadastro-despesa-recorrente`/`editar-despesa-recorrente`
e para `recurring-expense-form.util.spec.ts` — nenhum projeto de teste novo
em nenhuma das duas stacks

**Target Platform**: ASP.NET Core (Kestrel) + Angular SPA servida
separadamente — ambiente de desenvolvimento confiável, sem
autenticação/autorização/CORS nesta etapa (exceção de fase vigente do
Princípio IV, já aplicada aos demais endpoints)

**Project Type**: web — extensão dos projetos .NET (`backend/`) e Angular
(`frontend/`) já existentes na solution/workspace; nenhum projeto novo é
criado por esta feature

**Performance Goals**: Não especificado pela spec. Cada cadastro/edição
passa a gravar até ~13 linhas de `Occurrences` numa única transação
(`SaveChangesAsync`) em vez de 0–1 — volume ainda pequeno e limitado pelo
próprio teto de vigência de 1 ano (decisão 2 do refinamento elimina a
preocupação de geração ilimitada levantada em versões anteriores do
refinamento)

**Constraints**: Toda resposta (sucesso/erro) dentro do envelope único já
existente `ApiResponse<TData>`; mensagens de erro de forma/presença e de
negócio em PT-BR; nenhuma lógica de negócio na API layer ou na Application
layer (Princípios VI/XI/XII); as duas invariantes cruzadas (fim > início;
vigência ≤ 1 ano) e a invariante "fim não pode estar no passado" vivem
exclusivamente no Domain (decisão 5 do refinamento); uma edição de
`startDate`/`endDate` que viole qualquer invariante rejeita a requisição
inteira — nenhum campo é persistido (FR-009/FR-012); a migração de dados
existentes (User Story 4) não pode gerar nenhuma ocorrência retroativa
(FR-011)

**Scale/Scope**: Nenhum endpoint HTTP novo (3 já existentes ganham um
campo cada: `POST`/`GET`/`PUT` de `RecurringExpensesController`); ~6 novos
métodos/alterações de método no aggregate `RecurringExpense` + 1 método
novo em `ReferencePeriod`; 2 UseCases estendidos; 1 migração EF Core; 2
componentes Angular estendidos + 1 função nova em
`recurring-expense-form.util.ts`

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Avaliação |
|---|---|
| I — API-First Backend/Frontend Separation | PASS. `endDate` trafega exclusivamente pelos três endpoints HTTP já versionados (`/api/v1/recurring-expenses`); o frontend consome via `DespesaRecorrenteService` já existente, sem acesso direto a backend/BD. |
| II — Test-First Development | PASS (a verificar na implementação). Um teste por cenário de resposta declarado, nos quatro projetos de teste de backend já existentes + specs Angular já existentes (ver `research.md` §10) — nenhuma estrutura de pastas nova. `Infrastructure.Tests` ganha, pela primeira vez neste repositório, um teste de remoção de entidade rastreada (ver `research.md` §6), seguindo o mesmo precedente de validação manual contra PostgreSQL real já estabelecido por `007-edit-recurring-expense` (T041). |
| III — Type Safety & Static Analysis | PASS. Nenhuma mudança de configuração de projeto; `Nullable`/`TreatWarningsAsErrors` e o modo `strict` do TypeScript já habilitados em todos os projetos afetados; nenhum `any` introduzido. |
| IV — Secure Handling of Financial Data | PASS sob a exceção de fase vigente. Nenhuma auth/CORS implementada nos endpoints estendidos (mesmo débito já registrado para os endpoints existentes). |
| V — Simplicity & Incremental Delivery | PASS. Um método por invariante/operação de negócio (`ValidateVigencia`, `ValidateEndDateNotInPast`, `ChangeEndDate`), sem introduzir um `Update(...)` genérico; `ReferencePeriod.Next()` é a menor operação de avanço suficiente para os dois usos desta feature (sem `AddMonths(int)` genérico não utilizado); nenhum projeto novo, nenhuma dependência nova, nenhuma estrutura de pastas nova. |
| VI — Domain-Driven Design in the Domain Layer | PASS — núcleo desta feature. `_endDate` é um `CalendarDate` (Value Object), nunca um `DateOnly` cru; nenhum setter público é introduzido — a mutação passa por `ChangeEndDate`/`ChangeStartDate` (métodos de intenção de negócio); as três invariantes cruzadas vivem exclusivamente no aggregate, cada uma lançando `DomainRuleViolationException` com mensagem PT-BR já definida (`research.md` §2/§3); nenhuma validação de negócio duplicada na Application ou na Api. |
| VII — Infrastructure Layer | PASS, com um item a confirmar durante a implementação (não uma violação). `IRecurringExpenseRepository`/`RecurringExpenseRepository` mantêm a mesma assinatura; a única mudança de comportamento nova (remoção de uma `Occurrence` de um aggregate já rastreado) ainda não tem precedente neste repositório — `research.md` §6 já registra o risco e o teste de `Infrastructure.Tests` exigido para confirmá-lo antes de fechar a feature, seguindo o mesmo padrão de descoberta que `007-edit-recurring-expense` já usou para o caso equivalente de inserção (T041). `RepositoryManager`/`Lazy<T>` inalterados. |
| VIII/IX/X — Frontend (Standalone, Signals, DI) | PASS. Nenhum `NgModule` introduzido; `dataFim` é gerenciado por `signal`/`computed`, mesmo padrão já usado por `dataInicio` nos dois componentes; `HttpClient` continua injetado via `inject()` em `DespesaRecorrenteService` (inalterado); nenhuma mutação in-place de `input()`/estado de signal. |
| XI — Application Layer | PASS. Nenhuma regra de negócio nova na Application — `CreateRecurringExpenseUseCase`/`UpdateRecurringExpenseUseCase` continuam apenas orquestrando (parsear campos, invocar o aggregate, salvar); a única adição de "lógica" é o `try/catch` que traduz `DomainRuleViolationException` em `FieldError` por campo — mapeamento de exceção para forma de erro já é o papel estabelecido da Application neste projeto (idêntico ao já feito para `ArgumentException` dos Value Objects), não uma regra de negócio nova. `Input`/`Output` de cada UseCase seguem próprios, sem serem compartilhados entre si. |
| XII — API Layer Implementation | PASS. Nenhum endpoint novo, nenhuma versão nova; `[Required(ErrorMessage = "Data de fim é obrigatória.")]` em PT-BR nos dois `DataRequest`; `ProducesResponseType` dos três endpoints já cobre `400`/`404`/`500` — nenhum novo código de status introduzido; mapeamentos (`/Mappings`) recebem apenas mais um campo, sem lógica de negócio; middleware global de exceções inalterado (`DomainRuleViolationException` já é capturada pela Application antes de chegar à Api — nunca escapa como exceção não tratada). |

Nenhuma violação identificada. `Complexity Tracking` não é necessário.

## Project Structure

### Documentation (this feature)

```text
specs/009-data-fim-despesa-recorrente/
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
│   ├── Aggregates/
│   │   └── RecurringExpense.cs                     # MODIFICADO — campo _endDate, ValidateVigencia,
│   │                                                # ValidateEndDateNotInPast, ChangeEndDate, geração em
│   │                                                # lote (GenerateOccurrencesForVigencia), ChangeStartDate
│   │                                                # revalidado (ver data-model.md)
│   └── ValueObjects/
│       └── ReferencePeriod.cs                      # MODIFICADO — novo método Next()
├── Domain.Tests/
│   └── Aggregates/
│       └── RecurringExpenseTests.cs                # MODIFICADO — novos testes (ver research.md §10)
├── Application/
│   └── UseCases/
│       ├── CreateRecurringExpense/                 # MODIFICADO — EndDate no Input/Output, try/catch
│       │   ├── CreateRecurringExpenseUseCase.cs
│       │   ├── CreateRecurringExpenseUseCaseInput.cs
│       │   └── CreateRecurringExpenseUseCaseOutput.cs
│       ├── GetRecurringExpenseById/                # MODIFICADO — EndDate em RecurringExpenseData
│       │   └── GetRecurringExpenseByIdUseCaseOutput.cs
│       └── UpdateRecurringExpense/                 # MODIFICADO — EndDate no Input, ChangeStartDate/
│           │                                        # ChangeEndDate com try/catch (ver data-model.md)
│           ├── UpdateRecurringExpenseUseCase.cs
│           └── UpdateRecurringExpenseUseCaseInput.cs
├── Application.Tests/
│   └── UseCases/
│       ├── CreateRecurringExpense/                 # MODIFICADO — novos casos para endDate
│       └── UpdateRecurringExpense/                 # MODIFICADO — novos casos para endDate/startDate
├── Infrastructure/
│   ├── Configs/
│   │   └── RecurringExpenseConfigurations.cs       # MODIFICADO — mapeamento de _endDate
│   ├── Migrations/
│   │   └── <timestamp>_AddRecurringExpenseEndDate.cs  # NOVO — AddColumn + backfill SQL + AlterColumn
│   └── Repositories/
│       └── RecurringExpenseRepository.cs           # possível ajuste cirúrgico (ver research.md §6),
│                                                    # só se a verificação do teste abaixo confirmar
│                                                    # a necessidade
├── Infrastructure.Tests/
│   └── Repositories/
│       └── RecurringExpenseRepositoryTests.cs      # MODIFICADO — novo teste de regressão de remoção
│                                                    # de Occurrence rastreada (research.md §6)
├── Api/
│   ├── Requests/
│   │   ├── CreateRecurringExpenseDataRequest.cs    # MODIFICADO — EndDate [Required] PT-BR
│   │   └── UpdateRecurringExpenseDataRequest.cs    # MODIFICADO — EndDate [Required] PT-BR
│   ├── Responses/
│   │   ├── CreateRecurringExpenseDataResponse.cs   # MODIFICADO — EndDate
│   │   └── RecurringExpenseDataResponse.cs         # MODIFICADO — EndDate
│   └── Mappings/
│       ├── CreateRecurringExpenseDataRequestMapping.cs     # MODIFICADO
│       ├── CreateRecurringExpenseDataResponseMapping.cs    # MODIFICADO
│       ├── UpdateRecurringExpenseDataRequestMapping.cs     # MODIFICADO
│       └── RecurringExpenseDataResponseMapping.cs          # MODIFICADO
├── Api.Tests/
│   └── Controllers/
│       └── RecurringExpensesControllerTests.cs     # MODIFICADO — novos casos 400 para endDate
└── ContasEmDia.sln                                 # não modificada — nenhum projeto novo

frontend/
└── src/app/
    ├── features/despesa-recorrente/
    │   ├── despesa-recorrente.model.ts                     # MODIFICADO — endDate nos tipos de request/response
    │   ├── cadastro-despesa-recorrente/
    │   │   ├── cadastro-despesa-recorrente.component.ts     # MODIFICADO — signal/handlers/validação dataFim
    │   │   ├── cadastro-despesa-recorrente.component.html   # MODIFICADO — campo "Data de fim"
    │   │   └── cadastro-despesa-recorrente.component.spec.ts # MODIFICADO — novos casos
    │   └── editar-despesa-recorrente/
    │       ├── editar-despesa-recorrente.component.ts       # MODIFICADO — mesma adição da tela de cadastro
    │       ├── editar-despesa-recorrente.component.html     # MODIFICADO — campo "Data de fim"
    │       └── editar-despesa-recorrente.component.spec.ts  # MODIFICADO — novos casos
    └── shared/
        ├── recurring-expense-form.util.ts               # MODIFICADO — getDataFimError + addYearsIso
        └── recurring-expense-form.util.spec.ts          # MODIFICADO — novos casos

design/
└── Editar.dc.html                                  # possível atualização de texto/campo, espelhando
                                                     # Cadastro.dc.html (já atualizado nesta branch) —
                                                     # ver research.md §9
```

**Structure Decision**: Nenhum projeto novo é adicionado à solution
`.NET` nem ao workspace Angular. Esta feature estende os quatro projetos
backend já existentes (`Domain`, `Application`, `Infrastructure`, `Api`, e
seus respectivos `.Tests`) e as duas features de frontend já existentes
(`cadastro-despesa-recorrente`, `editar-despesa-recorrente`) mais o módulo
`shared` compartilhado entre elas, seguindo exatamente as mesmas
convenções de pastas e nomenclatura já usadas pelas features 001–008.

## Complexity Tracking

*Nenhuma violação da Constitution Check acima — seção não aplicável.*
