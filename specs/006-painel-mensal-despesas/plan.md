# Implementation Plan: Painel Mensal de Despesas

**Branch**: `006-painel-mensal-despesas` | **Date**: 2026-09-07 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/006-painel-mensal-despesas/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Entregar a tela "Painel mensal" (`design/Main.dc.html`) com dados reais,
seguindo fielmente o design de referência: cabeçalho de competência com
setas de navegação de mês funcionais e o botão "Nova despesa" (FR-020/
FR-021 — ver "Clarifications" de `spec.md`, sessão 2026-09-07), dois
banners condicionais (vencidas / vence em breve), três cartões de resumo,
lista de ocorrências e o fluxo completo de marcar/desfazer pagamento com
edição inline exclusiva. Isso exige, pela primeira vez neste repositório,
fechar as lacunas de domínio já registradas em
`refinements/painel-mensal-despesas.md` ("Pré-requisito"): a entidade
`Occurrence` ganha rastreamento de pagamento (valor pago, data de
pagamento) e um status derivado calculado a partir da data completa de
vencimento; o repositório de `RecurringExpense` ganha duas novas formas de
consulta (por competência de ocorrência, e pelo id de uma ocorrência
específica); e três novos endpoints HTTP (`GET`/`PATCH`/`DELETE`) sob um
novo recurso `occurrences`, todos versionados e reaproveitando o envelope
`ApiResponse<TData>` já fixado em `004-api-despesa-recorrente`. No
frontend, uma nova feature `painel-mensal-despesas` reproduz pixel-a-pixel
o layout do design usando Tailwind (mesma convenção já usada por
`cadastro-despesa-recorrente`) e Angular Signals, com uma pequena adição de
roteamento (Angular Router, já uma dependência do projeto) para que o
painel e o cadastro coexistam como duas telas navegáveis.

## Technical Context

**Language/Version**: C# / .NET 10 (Domain, Application, Infrastructure,
Api — mesmo target já usado em todo o backend); TypeScript ~6.0 / Angular
v22.1 (frontend)

**Primary Dependencies**: ASP.NET Core 10 Web API + `Swashbuckle.AspNetCore`
(já presentes, nenhuma dependência nova de backend); EF Core 10 com
`Npgsql.EntityFrameworkCore.PostgreSQL` (já presente); no frontend,
`@angular/router` (já listado em `package.json`, apenas não estava
utilizado ainda — não é uma dependência nova, ver `research.md` §7), Tailwind
CSS 4 (já presente), RxJS 7.8 (já presente). Nenhuma dependência nova, nem
de backend nem de frontend, é introduzida por esta feature.

**Storage**: PostgreSQL via EF Core (`ContasEmDiaDbContext`, já
implementado), com uma nova migration para as duas novas colunas de
`Occurrence` (`PaidAmount`, `PaymentDate`) — migrations existentes não são
alteradas (Princípio VII).

**Testing**: xUnit + `WebApplicationFactory<Program>` para o backend (um
teste por cenário de resposta declarado via `ProducesResponseType`, mesmo
padrão de `004-api-despesa-recorrente`); Vitest + Angular `TestBed` +
`HttpTestingController` para o frontend (mesmo padrão já usado por
`despesa-recorrente.service.spec.ts`).

**Target Platform**: ASP.NET Core (Kestrel) + Angular SPA, ambiente de
desenvolvimento confiável — sem autenticação/autorização/CORS nesta etapa
(exceção vigente do Princípio IV, inalterada por esta feature).

**Project Type**: web — mesma solution .NET (`backend/ContasEmDia.sln`) e
mesmo workspace Angular (`frontend/`) já existentes; nenhum projeto novo é
criado, apenas arquivos novos dentro dos projetos já existentes
(`Domain`, `Application`, `Infrastructure`, `Api` e seus `.Tests`, e a
feature `painel-mensal-despesas` dentro de `frontend/src/app/features/`).

**Performance Goals**: Não especificado pela spec — sem meta de performance
declarada (escopo pessoal/pequena escala, listagem não paginada por
decisão explícita da spec — ver "Assumptions").

**Constraints**: Toda resposta dentro do envelope único `ApiResponse<TData>`
(Princípio XII); status derivado calculado no backend a partir da data
completa de vencimento, nunca do número isolado do dia (FR-005/RF22,
EC06); apenas uma ocorrência em edição por vez no cliente (FR-012, estado
puramente client-side, sem persistência no servidor); substituição
silenciosa por padrões (valor previsto / data atual) ao confirmar
pagamento com valor/data em branco ou inválidos (FR-013), nunca bloqueio
com erro; mensagens de erro de negócio e de forma/presença sempre em PT-BR
(Princípios VI/XII); nenhuma autenticação/autorização/CORS (débito
explícito, Princípio IV); acessibilidade WCAG 2.1 AA em todo componente
novo (Princípio II), o que exige um pequeno desvio de fidelidade literal ao
markup do protótipo — ver `research.md` §8 (o link "Desfazer" do design é
um `<span onClick>`, não operável via teclado; a implementação real usa um
`<button>` com a mesma aparência visual).

**Scale/Scope**: 1 tela nova no frontend (`painel-mensal-despesas`), 3
endpoints HTTP novos (`GET /api/v1/occurrences`,
`PATCH /api/v1/occurrences/{occurrenceId}/payment`,
`DELETE /api/v1/occurrences/{occurrenceId}/payment`), 3 UseCases novos, 2
métodos de negócio novos em `Occurrence` + 3 métodos novos em
`RecurringExpense`, 3 métodos novos em `IRecurringExpenseRepository`, 1
migration EF Core nova, 1 rota Angular nova (`app.routes.ts`, 2 rotas:
painel e cadastro), 2 métodos novos de navegação no componente do painel
(`mesAnterior()`/`proximoMes()`, FR-020, reaproveitando o endpoint `GET`
já existente) e 1 `routerLink` no botão "Nova despesa" (FR-021, sem
endpoint ou rota novos além da já planejada `despesas/nova`).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Avaliação |
|---|---|
| I — API-First Backend/Frontend Separation | PASS. Os 3 endpoints novos são a única forma pela qual o frontend acessa dados de pagamento/painel; nenhum acesso direto a banco. |
| II — Test-First Development | PASS (a verificar na implementação/`tasks.md`). Backend: `WebApplicationFactory`, um teste por cenário de `ProducesResponseType`, sem pasta de testes dedicada além da já existente (`Api.Tests/Controllers`). Frontend: Vitest, cobertura das regras de status derivado, edição exclusiva, banners condicionais e acessibilidade (labels, `role="alert"`, operabilidade via teclado do botão "Desfazer" — ver `research.md` §8). |
| III — Type Safety & Static Analysis | PASS. C# com nullable + warnings-as-errors (já configurado nos projetos existentes); TypeScript strict, sem `any` novo. |
| IV — Secure Handling of Financial Data | PASS sob a exceção de fase vigente — nenhuma auth/CORS nova, comportamento inalterado. |
| V — Simplicity & Incremental Delivery | PASS. Sem paginação (fora de escopo por decisão da spec), sem biblioteca de Unit of Work nova (reutiliza o padrão repositório-chama-`SaveChangesAsync` já usado por `AddAsync`), sem nova hierarquia de exceções além de um único tipo (`DomainRuleViolationException`) justificado por uma necessidade concreta e presente — ver `research.md` §5. Roteamento Angular introduzido com o mínimo necessário (2 rotas), usando uma dependência já presente no projeto. Navegação de mês (FR-020) e botão "Nova despesa" (FR-021) reaproveitam, respectivamente, o mesmo endpoint `GET` já existente e o mesmo router já introduzido — dois métodos de componente e um `routerLink`, nenhuma rota, endpoint ou dependência nova. |
| VI — DDD no Domain | PASS. `Occurrence` ganha métodos de intenção de negócio (`MarkAsPaid`, `UndoPayment`, `GetDerivedStatus`) em vez de setters; novo Value Object `OccurrenceDerivedStatus` segue exatamente o mesmo padrão de todo enum já existente no Domain (wrapper validando `Enum.IsDefined`); toda exceção de regra de negócio nova carrega mensagem PT-BR; construtor privado de EF Core de `Occurrence` não é afetado. |
| VII — Infrastructure Layer | PASS. `IRecurringExpenseRepository` ganha 3 métodos novos, implementados em `RecurringExpenseRepository`; nova migration aditiva (não altera migrations existentes); `OccurrenceConfigurations` ganha mapeamento das 2 colunas novas; nenhum Unit of Work novo introduzido (`UpdateAsync` apenas chama `SaveChangesAsync()`, mesmo padrão de `AddAsync`). |
| VIII — Angular Standalone Architecture & Project Structure | PASS. Nova feature em pasta própria (`features/painel-mensal-despesas/`), componentes standalone, Tailwind para estilo (mesma convenção de `cadastro-despesa-recorrente`, cores fora do tema compartilhado usadas via classes arbitrárias `bg-[#hex]`, mesmo padrão já em uso). |
| IX — Signal-Based Reactivity & HTTP Access | PASS. Estado do componente (`occurrences`, `editingId`, rascunhos de edição) em `signal`/`computed`; `HttpClient` injetado via `inject()` em um serviço `providedIn: 'root'`. |
| X — Dependency Injection & Frontend Coding Standards | PASS. `inject()` em vez de injeção por construtor; nomes de arquivo seguindo convenção (`*.component.ts`, `*.service.ts`); inputs/signals tratados como imutáveis (atualizações via `set`/`update`). |
| XI — Application Layer Implementation | PASS. 3 UseCases novos, cada um em sua própria pasta com `I<Nome>UseCase`/`<Nome>UseCase`/Input/Output dedicados; UseCases apenas orquestram (buscam o agregado, chamam o método de negócio, chamam `UpdateAsync`/persistem); nenhuma regra de negócio nova implementada na Application (a substituição de valor/data em branco é preenchimento de padrão de orquestração, não uma regra de invariante — a regra "não pode marcar como paga uma ocorrência já paga" continua exclusivamente no Domain). Application continua dependendo apenas do Domain. |
| XII — API Layer Implementation | PASS. Rota versionada (`api/v1/occurrences`); `DataRequest`/`DataResponse` dedicados por ação; envelope único; `ProducesResponseType` completo por ação (`200`/`400`/`404`/`500` conforme o caso); mapeamentos dedicados um por tipo; middleware global de exceções estendido (não duplicado por controller) para também mapear `DomainRuleViolationException` → `400` e `KeyNotFoundException` → `404`, além do `500` genérico já existente; validação de forma/presença dos dois campos opcionais de pagamento não usa data annotations (ambos são opcionais por definição — FR-013), portanto não há `ErrorMessage` de forma a declarar aqui; sem auth/CORS. |

Nenhuma violação identificada. `Complexity Tracking` não é necessário — a
única adição de tipo novo além dos UseCases/DataRequest/DataResponse
esperados (`DomainRuleViolationException`) está justificada em
`research.md` §5 por uma necessidade concreta (distinguir, no middleware
global, uma violação de regra de negócio de qualquer outra
`InvalidOperationException` de infraestrutura — já existe um teste
existente, `ExceptionHandlingMiddlewareTests`, que depende de uma
`InvalidOperationException` de persistência continuar mapeando para
`500`, o que impede reutilizar esse tipo do BCL para o novo mapeamento de
`400`).

## Project Structure

### Documentation (this feature)

```text
specs/006-painel-mensal-despesas/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/
│   └── api-contract.md  # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── Domain/
│   ├── Entities/
│   │   └── Occurrence.cs                       # MODIFICADO — MarkAsPaid, UndoPayment, GetDerivedStatus, GetPaidAmount, GetPaymentDate
│   ├── Aggregates/
│   │   └── RecurringExpense.cs                 # MODIFICADO — FindOccurrence, GetOccurrencesForPeriod, MarkOccurrenceAsPaid, UndoOccurrencePayment
│   ├── ValueObjects/
│   │   └── OccurrenceDerivedStatus.cs           # NOVO — enum + wrapper (Paid/Overdue/DueSoon/Pending)
│   ├── Repositories/
│   │   └── IRecurringExpenseRepository.cs       # MODIFICADO — GetByReferencePeriodAsync, GetByOccurrenceIdAsync, UpdateAsync
│   └── DomainRuleViolationException.cs          # NOVO — tipo único para violações de regra de negócio (ver research.md §5)
├── Domain.Tests/
│   ├── Entities/
│   │   └── OccurrenceTests.cs                   # NOVO
│   ├── Aggregates/
│   │   └── RecurringExpenseTests.cs             # MODIFICADO
│   └── ValueObjects/
│       └── OccurrenceDerivedStatusTests.cs      # NOVO
├── Application/
│   └── UseCases/
│       ├── GetMonthlyPanel/                     # NOVO
│       ├── MarkOccurrenceAsPaid/                # NOVO
│       └── UndoOccurrencePayment/               # NOVO
├── Application.Tests/
│   └── UseCases/
│       ├── GetMonthlyPanel/                     # NOVO
│       ├── MarkOccurrenceAsPaid/                # NOVO
│       └── UndoOccurrencePayment/               # NOVO
├── Infrastructure/
│   ├── Repositories/
│   │   └── RecurringExpenseRepository.cs        # MODIFICADO — 3 métodos novos
│   ├── Configs/
│   │   └── OccurrenceConfigurations.cs          # MODIFICADO — PaidAmount/PaymentDate
│   └── Migrations/
│       └── <timestamp>_AddOccurrencePaymentTracking.cs  # NOVO
├── Infrastructure.Tests/
│   └── Repositories/                            # MODIFICADO — cobertura dos 3 métodos novos
├── Api/
│   ├── Controllers/
│   │   └── OccurrencesController.cs             # NOVO — GET/PATCH/DELETE
│   ├── Requests/
│   │   └── MarkOccurrenceAsPaidDataRequest.cs   # NOVO
│   ├── Responses/
│   │   ├── GetMonthlyPanelDataResponse.cs       # NOVO
│   │   ├── PanelOccurrenceDataResponse.cs       # NOVO
│   │   ├── MarkOccurrenceAsPaidDataResponse.cs  # NOVO
│   │   └── UndoOccurrencePaymentDataResponse.cs # NOVO
│   ├── Mappings/
│   │   ├── GetMonthlyPanelDataResponseMapping.cs        # NOVO
│   │   ├── MarkOccurrenceAsPaidDataRequestMapping.cs    # NOVO
│   │   ├── MarkOccurrenceAsPaidDataResponseMapping.cs   # NOVO
│   │   └── UndoOccurrencePaymentDataResponseMapping.cs  # NOVO
│   ├── Middlewares/
│   │   └── ExceptionHandlingMiddleware.cs       # MODIFICADO — + 400 (DomainRuleViolationException) e 404 (KeyNotFoundException)
│   └── Program.cs                                # MODIFICADO — DI dos 3 UseCases novos; ApiBehaviorOptions generalizado para ApiResponse<object> (ver research.md §6)
└── Api.Tests/
    ├── Controllers/
    │   └── OccurrencesControllerTests.cs        # NOVO
    └── ExceptionHandlingMiddlewareTests.cs       # MODIFICADO — + cenários 400/404

frontend/src/app/
├── app.routes.ts                                 # NOVO — '' → Painel, 'despesas/nova' → Cadastro
├── app.config.ts                                 # MODIFICADO — provideRouter(routes)
├── app.ts                                        # MODIFICADO — RouterOutlet em vez do componente fixo
├── app.html                                      # MODIFICADO — <router-outlet />
├── app.spec.ts                                   # MODIFICADO — reflete a nova composição via router
└── features/
    ├── despesa-recorrente/                       # já existe — não modificado (fora de escopo)
    └── painel-mensal-despesas/                   # NOVO
        ├── painel-mensal-despesas.model.ts
        ├── painel-mensal-despesas.service.ts
        ├── painel-mensal-despesas.service.spec.ts
        └── painel-mensal-despesas.component.{ts,html,spec.ts}
```

**Structure Decision**: Mesma solution .NET em camadas (Domain → Application/
Infrastructure → Api) e mesmo workspace Angular por feature já
estabelecidos pelas features anteriores; nenhum projeto novo é criado. A
única decisão estrutural nova é a introdução de `app.routes.ts` no
frontend (usando `@angular/router`, já uma dependência do `package.json`,
portanto sem violar a salvaguarda de dependências novas do AI Agent
Guardrails), necessária para que a nova tela de painel e a tela de
cadastro já existente continuem ambas alcançáveis a partir de `App` — ver
`research.md` §7 para as alternativas descartadas.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

Nenhuma violação da Constitution Check acima — seção não aplicável.
