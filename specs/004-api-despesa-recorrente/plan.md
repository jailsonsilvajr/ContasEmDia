# Implementation Plan: API de Despesa Recorrente (Cadastro)

**Branch**: `004-api-despesa-recorrente` | **Date**: 2026-09-05 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/004-api-despesa-recorrente/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Criar o primeiro projeto de API do repositório (`backend/Api`), expondo um
único endpoint `POST /api/v1/recurring-expenses` que traduz o corpo da
requisição em `CreateRecurringExpenseUseCaseInput`, delega toda a
orquestração a `ICreateRecurringExpenseUseCase.ExecuteAsync` (já
implementado em `backend/Application`) e traduz o `Output` resultante em uma
resposta HTTP — sem introduzir nenhuma regra de negócio nova. Por ser o
primeiro endpoint do repositório, esta feature também fixa, pela primeira
vez, as convenções obrigatórias do Princípio XII da constituição para toda
API futura: envelope de resposta único (`ApiResponse<TData>`/`ApiError`),
versionamento de rota por prefixo literal, middleware global de tratamento
de exceções, e documentação OpenAPI/SwaggerUI. Inclui também dois
pré-requisitos diretos: a implementação concreta de `ICurrentDateProvider`
na Infrastructure (`SystemCurrentDateProvider`) e a atualização dos
documentos de contrato de API já assumidos pelo frontend, hoje desatualizados
frente a estas decisões.

## Technical Context

**Language/Version**: C# / .NET 10 (mesmo target já usado por Domain,
Application e Infrastructure)

**Primary Dependencies**: ASP.NET Core Web API (SDK `Microsoft.NET.Sdk.Web`);
`Swashbuckle.AspNetCore` para geração do documento OpenAPI e SwaggerUI (nova
dependência desta feature — ver `research.md` §1); `Microsoft.EntityFrameworkCore.SqlServer`
10 (já usado por Infrastructure, referenciado transitivamente)

**Storage**: SQL Server via EF Core 10 (`ContasEmDiaDbContext`, já
implementado em Infrastructure), connection string via `appsettings.json`/
`appsettings.Development.json` — nunca hardcoded (FR-014, Princípio IV)

**Testing**: xUnit + `WebApplicationFactory<Program>` (novo projeto
`backend/Api.Tests`, seguindo o padrão já usado por `Application.Tests`/
`Domain.Tests`/`Infrastructure.Tests`)

**Target Platform**: ASP.NET Core (Kestrel), ambiente de desenvolvimento
confiável — sem autenticação/autorização/CORS nesta etapa (exceção vigente
do Princípio IV)

**Project Type**: web — novo projeto `backend/Api` dentro da solution
.NET já existente (`backend/ContasEmDia.sln`), consumido futuramente pelo
frontend Angular já existente em `frontend/`

**Performance Goals**: Não especificado pela spec — nenhuma meta de
performance declarada para esta feature (endpoint único, sem carga
concorrente relevante nesta etapa)

**Constraints**: Toda resposta (sucesso/erro) dentro do envelope único
`ApiResponse<TData>`; mensagens de erro de forma/presença e de negócio em
PT-BR; rota sob `/api/v1/...`; nenhuma lógica de negócio na API layer
(Princípio XII); nenhuma autenticação/autorização/CORS (débito explícito,
Princípio IV)

**Scale/Scope**: Um único endpoint HTTP, primeiro projeto de API do
repositório — escopo estritamente limitado ao cadastro de despesa
recorrente (nenhuma listagem/detalhe/edição/exclusão)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Avaliação |
|---|---|
| I — API-First Backend/Frontend Separation | PASS. Esta feature entrega exatamente a API versionada exigida; o frontend seguirá consumindo-a exclusivamente via HTTP (nenhum acesso direto a backend/BD). |
| II — Test-First Development | PASS (a verificar na implementação). `Api.Tests` usará `WebApplicationFactory`, um teste por cenário de resposta (`201`×2, `400`×2, `500`), sem estrutura de pastas adicional não justificada pelo número de endpoints (apenas 1). |
| III — Type Safety & Static Analysis | PASS. `backend/Api.csproj` habilitará `Nullable` e `TreatWarningsAsErrors`, mesma configuração dos demais projetos backend. |
| IV — Secure Handling of Financial Data | PASS sob a exceção de fase vigente. Nenhuma auth/CORS implementada (débito explícito, FR-013); connection string via configuração, nunca hardcoded (FR-014). |
| V — Simplicity & Incremental Delivery | PASS. Sem biblioteca de versionamento dedicada (prefixo literal), sem estrutura de testes especulativa, sem abstrações além do necessário para 1 endpoint. |
| VI — DDD no Domain | N/A (não modificado por esta feature). |
| VII — Infrastructure Layer | PASS. Única adição é `SystemCurrentDateProvider` (implementação de uma Port já definida em Application), sem alterar Repository/Migrations/Configs/Contexts existentes. |
| XI — Application Layer | PASS. Application não é alterado; a API apenas consome `ICreateRecurringExpenseUseCase` já implementado. |
| XII — API Layer Implementation | PASS — é o objeto central desta feature. Rota versionada (FR-002), `DataRequest`/`DataResponse` dedicados (Princípio XII), mapeamentos dedicados um-por-tipo (Princípio XII), envelope único (FR-006), `ProducesResponseType` completo (FR-010), middleware global de exceções (FR-011), OpenAPI/SwaggerUI (FR-012), validação de forma/presença apenas, com `ErrorMessage` PT-BR (FR-003/FR-004), sem auth/CORS (FR-013). |

Nenhuma violação identificada. `Complexity Tracking` não é necessário.

## Project Structure

### Documentation (this feature)

```text
specs/004-api-despesa-recorrente/
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
├── Domain/                          # já existe — não modificado
├── Domain.Tests/                    # já existe — não modificado
├── Application/                     # já existe — não modificado
├── Application.Tests/               # já existe — não modificado
├── Infrastructure/                  # já existe
│   ├── Repositories/                # já existe — não modificado
│   ├── Contexts/                    # já existe — não modificado
│   ├── Migrations/                  # já existe — não modificado
│   ├── Configs/                     # já existe — não modificado
│   └── SystemCurrentDateProvider.cs # NOVO (FR-015) — implementação concreta de ICurrentDateProvider
├── Infrastructure.Tests/            # já existe — cobertura de SystemCurrentDateProvider adicionada aqui
├── Api/                              # NOVO — projeto desta feature
│   ├── ContasEmDia.Api.csproj
│   ├── Program.cs                    # composição de DI, ApiBehaviorOptions, Swagger, middleware
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Controllers/
│   │   └── RecurringExpensesController.cs
│   ├── Requests/
│   │   └── CreateRecurringExpenseDataRequest.cs
│   ├── Responses/
│   │   ├── ApiResponse.cs
│   │   ├── ApiError.cs
│   │   ├── CreateRecurringExpenseDataResponse.cs
│   │   ├── OccurrenceDataResponse.cs
│   │   └── ReferencePeriodDataResponse.cs
│   ├── Mappings/
│   │   ├── CreateRecurringExpenseDataRequestMapping.cs
│   │   └── CreateRecurringExpenseDataResponseMapping.cs
│   ├── Middlewares/
│   │   └── ExceptionHandlingMiddleware.cs
│   └── Filters/                      # vazia nesta etapa (convenção fixada para uso futuro)
├── Api.Tests/                        # NOVO — testes de integração do endpoint
│   └── ContasEmDia.Api.Tests.csproj
└── ContasEmDia.sln                   # atualizada com os 2 novos projetos (Api, Api.Tests)

specs/002-cadastro-despesa-recorrente/contracts/api-contract.md  # atualizado (FR-016)
refinements/frontend/cadastro-despesa-recorrente.md              # atualizado (FR-016, seção "Contrato de API necessário")

frontend/                             # já existe — nenhum código de frontend alterado por esta feature
```

**Structure Decision**: Projeto web multi-camada .NET já estabelecido
(`backend/ContasEmDia.sln`, camadas Domain → Application/Infrastructure →
Api). Esta feature adiciona dois novos projetos à solution
(`backend/Api`, `backend/Api.Tests`), seguindo exatamente a mesma convenção
de nomenclatura e organização de pastas por camada já usada por
Domain/Application/Infrastructure, mais a única classe nova em
Infrastructure exigida por FR-015. `Api` é a raiz de composição (DI) da
aplicação e a única camada com permissão para referenciar tanto Application
quanto Infrastructure diretamente (papel distinto do Princípio XI, que
restringe apenas o projeto Application a depender só do Domain).

## Complexity Tracking

*Nenhuma violação da Constitution Check acima — seção não aplicável.*
