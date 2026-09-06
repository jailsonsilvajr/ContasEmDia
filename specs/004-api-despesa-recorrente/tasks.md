---

description: "Task list for API de Despesa Recorrente (Cadastro)"
---

# Tasks: API de Despesa Recorrente (Cadastro)

**Input**: Design documents from `/specs/004-api-despesa-recorrente/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-contract.md, quickstart.md

**Tests**: Required by this project's constitution (Principle II — Test-First Development). `Api.Tests` (xUnit + `WebApplicationFactory<Program>`) tasks are written before the implementation task they verify, following Red-Green-Refactor, with one test per distinct response scenario the endpoint can produce.

**Organization**: Tasks are grouped by user story (spec.md priorities P1–P4). This feature exposes exactly one controller action; each user story adds a distinct branch/response of that same action (success, business-rule failure, shape/presence failure, documentation), so later stories extend files created by earlier ones rather than being fully independent implementations — each phase remains independently *testable* per its Independent Test criterion in spec.md.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Tasks that append scenarios to the same file are listed sequentially (not `[P]`) even when logically independent, to avoid edit conflicts
- Paths are relative to the repository root (`backend/...`, `specs/...`, `refinements/...`)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Scaffold the two new sibling projects (`Api`, `Api.Tests`) and add them to the existing `backend/ContasEmDia.sln`, following the same layering convention already used by `Domain`/`Application`/`Infrastructure`.

- [X] T001 [P] Create `backend/Api/ContasEmDia.Api.csproj` scaffold: `Microsoft.NET.Sdk.Web`, `TargetFramework net10.0`, `ImplicitUsings enable`, `Nullable enable`, `TreatWarningsAsErrors true`, `PackageReference Swashbuckle.AspNetCore` (research.md §1), `ProjectReference` to `../Application/ContasEmDia.Application.csproj` and `../Infrastructure/ContasEmDia.Infrastructure.csproj`; plus `backend/Api/appsettings.json` and `backend/Api/appsettings.Development.json` with an empty/placeholder SQL Server connection string key — never hardcoded (FR-014)
- [X] T002 [P] Create `backend/Api.Tests/ContasEmDia.Api.Tests.csproj` scaffold: xUnit (`coverlet.collector`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `Using Include="Xunit"`, `IsPackable false`), plus `Microsoft.AspNetCore.Mvc.Testing` and `Microsoft.EntityFrameworkCore.InMemory` (research.md §6), with a `ProjectReference` to `../Api/ContasEmDia.Api.csproj`
- [X] T003 Add `Api` and `Api.Tests` projects to `backend/ContasEmDia.sln` (depends on T001, T002)

**Checkpoint**: `dotnet build backend/ContasEmDia.sln` succeeds with two new empty projects.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The shared response envelope, the missing `ICurrentDateProvider` implementation (FR-015), the global exception middleware (FR-011), the DI composition root, and the shared test fixture — every user story's controller action and tests depend on all five. **No User Story implementation can begin until this phase is complete.**

- [X] T004 [P] Create `ApiResponse<TData>` envelope type (`Success` bool, `Data` `TData?`, `Errors` `IReadOnlyCollection<ApiError>?`, static `Success(TData)`/`Failure(IReadOnlyCollection<ApiError>)` factories) in `backend/Api/Responses/ApiResponse.cs` (FR-006, data-model.md)
- [X] T005 [P] Create `ApiError` record (`Field` `string?`, `Message` `string`) in `backend/Api/Responses/ApiError.cs` (data-model.md)
- [X] T006 [P] Write failing unit test asserting `SystemCurrentDateProvider.GetCurrentDate()` returns `DateOnly.FromDateTime(DateTime.Now)` in `backend/Infrastructure.Tests/SystemCurrentDateProviderTests.cs` (FR-015)
- [X] T007 Implement `SystemCurrentDateProvider` (`ICurrentDateProvider`) returning `DateOnly.FromDateTime(DateTime.Now)` in `backend/Infrastructure/SystemCurrentDateProvider.cs` (depends on T006) (FR-015)
- [X] T008 [P] Implement `ExceptionHandlingMiddleware` in `backend/Api/Middlewares/ExceptionHandlingMiddleware.cs`: catch any unhandled exception and respond `500 Internal Server Error` with `ApiResponse<object>.Failure(...)` containing one generic `ApiError` (`Field = null`), no internal exception details in the body (FR-011) (depends on T004, T005)
- [X] T009 Create `backend/Api/Program.cs` composition root: `ContasEmDiaDbContext` registered via `IConfiguration` connection string (SQL Server, never hardcoded — FR-014), `IRepositoryManager` → `RepositoryManager`, `ICreateRecurringExpenseUseCase` → `CreateRecurringExpenseUseCase`, `ICurrentDateProvider` → `SystemCurrentDateProvider`, `AddControllers()`, `app.UseMiddleware<ExceptionHandlingMiddleware>()`, `app.MapControllers()`, and a trailing `public partial class Program;` marker required by `WebApplicationFactory<Program>` (FR-014) (depends on T007, T008)
- [X] T010 Create `CustomWebApplicationFactory` test fixture in `backend/Api.Tests/CustomWebApplicationFactory.cs`: a `WebApplicationFactory<Program>` that replaces `ContasEmDiaDbContext`'s registration with the EF Core InMemory provider so tests never touch a real database (research.md §6) (depends on T009)

**Checkpoint**: `dotnet build backend/ContasEmDia.sln` succeeds; the DI graph resolves end-to-end; `CustomWebApplicationFactory` can produce a test client against an isolated in-memory database. No endpoint exists yet — user story implementation can now begin.

---

## Phase 3: User Story 1 - Cadastrar despesa recorrente com sucesso via API (Priority: P1) 🎯 MVP

**Goal**: A valid request body is translated to `CreateRecurringExpenseUseCaseInput`, delegated to `ICreateRecurringExpenseUseCase.ExecuteAsync`, and a successful `Output` is returned as `201 Created` inside the shared envelope, including the current-month occurrence when one was generated.

**Independent Test**: `POST` a valid body via `WebApplicationFactory` (no UI) and verify the response is `201 Created` with the success envelope populated from the Use Case result.

### Tests for User Story 1

> Write these tests first; they must fail (no controller/route exists yet) before T020 is implemented.

- [X] T011 [US1] Integration test: `POST /api/v1/recurring-expenses` with a valid Active body starting in the current competência → `201 Created`, `success: true`, `data.occurrences` with exactly 1 item, in `backend/Api.Tests/Controllers/RecurringExpensesControllerTests.cs` (Acceptance Scenario 1) (depends on T010)
- [X] T012 [US1] Add test to `RecurringExpensesControllerTests.cs`: valid Paused body, or Active with a future-competência start → `201 Created`, `data.occurrences` an empty list (Acceptance Scenario 2)
- [X] T013 [US1] Add test to `RecurringExpensesControllerTests.cs`: assert every JSON field name of the success response is `camelCase`, matching `contracts/api-contract.md` (Acceptance Scenario 3)

### Implementation for User Story 1

- [X] T014 [P] [US1] Create `CreateRecurringExpenseDataRequest` record in `backend/Api/Requests/CreateRecurringExpenseDataRequest.cs`: `Name`/`Category`/`StartDate`/`Frequency`/`Status` as `string?`, `MonthlyAmount` as `decimal?`, `DueDay` as `int?`, each with `[Required(ErrorMessage = "...")]` in PT-BR per data-model.md's table; `Note` optional, no annotation (FR-003, data-model.md, "Presença de campos numéricos exige tipos anuláveis")
- [X] T015 [P] [US1] Create `ReferencePeriodDataResponse` record (`Year`, `Month`) in `backend/Api/Responses/ReferencePeriodDataResponse.cs` (data-model.md)
- [X] T016 [P] [US1] Create `OccurrenceDataResponse` record (`Id`, `ReferencePeriod`, `DueDate`, `Status`, `Name`, `Category`, `ExpectedAmount`) in `backend/Api/Responses/OccurrenceDataResponse.cs` (data-model.md)
- [X] T017 [P] [US1] Create `CreateRecurringExpenseDataResponse` record (`Id`, `Name`, `Category`, `MonthlyAmount`, `DueDay`, `StartDate`, `Frequency`, `Status`, `Note`, `Occurrences: IReadOnlyCollection<OccurrenceDataResponse>`) in `backend/Api/Responses/CreateRecurringExpenseDataResponse.cs` (data-model.md)
- [X] T018 [P] [US1] Create `CreateRecurringExpenseDataRequestMapping` (`CreateRecurringExpenseDataRequest` → `CreateRecurringExpenseUseCaseInput`, converting `MonthlyAmount`/`DueDay` from `decimal?`/`int?` to their non-nullable equivalents) in `backend/Api/Mappings/CreateRecurringExpenseDataRequestMapping.cs` (Princípio XII) (depends on T014)
- [X] T019 [P] [US1] Create `CreateRecurringExpenseDataResponseMapping` with a success-path method (`CreateRecurringExpenseUseCaseOutput` → `CreateRecurringExpenseDataResponse`, re-nesting `OccurrenceData.ReferenceYear`/`ReferenceMonth` into `ReferencePeriodDataResponse`) in `backend/Api/Mappings/CreateRecurringExpenseDataResponseMapping.cs` (Princípio XII, data-model.md "Forma da ocorrência na resposta") (depends on T015, T016, T017)
- [X] T020 [US1] Implement `RecurringExpensesController` in `backend/Api/Controllers/RecurringExpensesController.cs`: `[ApiController]`, `[Route("api/v1/recurring-expenses")]`, `POST` action taking `CreateRecurringExpenseDataRequest`, calling `CreateRecurringExpenseDataRequestMapping` → `ICreateRecurringExpenseUseCase.ExecuteAsync` → on `IsSuccess == true`, `CreateRecurringExpenseDataResponseMapping` → `201 Created` with `ApiResponse<CreateRecurringExpenseDataResponse>.Success(...)`; declare `[ProducesResponseType(StatusCodes.Status201Created, ...)]` (FR-001, FR-002, FR-005, FR-006, FR-007) (depends on T018, T019)

**Checkpoint**: T011–T013 pass. User Story 1 is fully functional and independently testable.

---

## Phase 4: User Story 2 - Receber erro padronizado ao violar uma regra de negócio (Priority: P2)

**Goal**: A formally valid body that violates a closed Domain rule is answered `400 Bad Request` in the same envelope, one `ApiError` per invalid field, PT-BR messages relayed as-is from the Use Case.

**Independent Test**: `POST` a body with one or more business-rule violations and verify `400 Bad Request` with `Success = false`, `Data = null`, and one error per invalid field.

### Tests for User Story 2

- [X] T021 [US2] Add test to `RecurringExpensesControllerTests.cs`: exactly one business-rule violation (e.g. `category: "CategoriaInexistente"`) → `400 Bad Request`, `success: false`, `data: null`, one `ApiError` with the field name and PT-BR message (Acceptance Scenario 1)
- [X] T022 [US2] Add test to `RecurringExpensesControllerTests.cs`: more than one simultaneous business-rule violation → `400 Bad Request` with all errors aggregated in the same list, and nothing persisted (Acceptance Scenario 2)

### Implementation for User Story 2

- [X] T023 [US2] Add a failure-path method to `CreateRecurringExpenseDataResponseMapping` (`IReadOnlyCollection<FieldError>` → `IReadOnlyCollection<ApiError>`, direct field-by-field transposition, no rewriting) in `backend/Api/Mappings/CreateRecurringExpenseDataResponseMapping.cs` (FR-008, "Mapeamentos dedicados") (depends on T019)
- [X] T024 [US2] Extend the `RecurringExpensesController` action: on `IsSuccess == false`, return `400 Bad Request` with `ApiResponse<CreateRecurringExpenseDataResponse>.Failure(...)` built from T023's mapping; add `[ProducesResponseType(StatusCodes.Status400BadRequest, ...)]` in `backend/Api/Controllers/RecurringExpensesController.cs` (FR-008, FR-010) (depends on T023)

**Checkpoint**: T021–T022 pass. User Stories 1 and 2 both work independently.

---

## Phase 5: User Story 3 - Receber o mesmo formato de erro para dados malformados ou ausentes (Priority: P3)

**Goal**: A shape/presence failure of `CreateRecurringExpenseDataRequest` itself (e.g. a missing required field) is answered `400 Bad Request` using the exact same envelope as User Story 2 — never ASP.NET Core's native `ValidationProblemDetails`.

**Independent Test**: `POST` a body with a missing required field and verify `400 Bad Request` in the same error envelope as the business-rule scenario, never the framework's default format.

### Tests for User Story 3

- [X] T025 [US3] Add test to `RecurringExpensesControllerTests.cs`: JSON body missing the required `name` field → `400 Bad Request`, same envelope as US2, `errors: [{ field: "name", message: "Nome é obrigatório." }]` (Acceptance Scenario 1)
- [X] T026 [US3] Add test to `RecurringExpensesControllerTests.cs`: JSON body missing the required numeric field `monthlyAmount` → `400 Bad Request` shape/presence error, never silently treated as `0` (Edge Case)
- [X] T027 [US3] Add test to `RecurringExpensesControllerTests.cs`: for any shape/presence failure, assert the response is never ASP.NET Core's native `ValidationProblemDetails` format and every message is in PT-BR (Acceptance Scenario 2)

### Implementation for User Story 3

- [X] T028 [US3] Configure `ApiBehaviorOptions.InvalidModelStateResponseFactory` in `backend/Api/Program.cs`: build `ApiResponse<CreateRecurringExpenseDataResponse>.Failure(...)` from the invalid `ModelStateDictionary`, one `ApiError` per invalid field, `400 Bad Request` (FR-009, "Substituição da resposta automática de ModelState inválido") (depends on T009, T004, T005)

**Checkpoint**: T025–T027 pass. User Stories 1, 2, and 3 are independently functional with one consistent error envelope.

---

## Phase 6: User Story 4 - Explorar e testar o endpoint via documentação interativa (Priority: P4)

**Goal**: A machine-readable OpenAPI document and an interactive SwaggerUI expose the endpoint, its request/response types, and all three possible outcomes (`201`/`400`/`500`).

**Independent Test**: With the API running, access the OpenAPI document and the SwaggerUI, and verify the endpoint and every declared response appear.

### Tests for User Story 4

- [X] T029 [US4] Integration test: `GET /swagger/v1/swagger.json` returns a valid OpenAPI document describing `POST /api/v1/recurring-expenses`, `CreateRecurringExpenseDataRequest`, and `ApiResponse<CreateRecurringExpenseDataResponse>` in `backend/Api.Tests/SwaggerDocumentTests.cs` (Acceptance Scenario 1) (depends on T010)
- [X] T030 [US4] Add test to `SwaggerDocumentTests.cs`: the OpenAPI document declares `201`, `400`, and `500` as documented responses for the endpoint (Acceptance Scenario 2)

### Implementation for User Story 4

- [X] T031 [P] [US4] Add `[ProducesResponseType(StatusCodes.Status500InternalServerError, ...)]` for `ApiResponse<CreateRecurringExpenseDataResponse>` to the controller action in `backend/Api/Controllers/RecurringExpensesController.cs`, completing FR-010's full `201`/`400`/`500` declaration
- [X] T032 [P] [US4] Register Swashbuckle in `backend/Api/Program.cs`: `AddEndpointsApiExplorer()`, `AddSwaggerGen()`, `app.UseSwagger()`, `app.UseSwaggerUI()` (FR-012) (depends on T009, T028)

**Checkpoint**: T029–T030 pass. All four user stories are independently functional; SwaggerUI/OpenAPI documents the full contract.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Cover the one remaining Edge Case not tied to a priority story (the `500` safety net), close FR-016's documentation debt, and run final verification against quickstart.md.

- [X] T033 [P] Integration test (Edge Case): substitute a throwing dependency (e.g. a fake `IRepositoryManager`/`IRecurringExpenseRepository` that throws) via a dedicated `WebApplicationFactory` override → `500 Internal Server Error` with the generic `ApiResponse` envelope, no internal exception details in the body, in `backend/Api.Tests/ExceptionHandlingMiddlewareTests.cs`
- [X] T034 [P] Update `specs/002-cadastro-despesa-recorrente/contracts/api-contract.md` to the versioned route (`POST /api/v1/recurring-expenses`) and the `ApiResponse<TData>`/`ApiError` envelope defined by this feature (FR-016)
- [X] T035 [P] Update the "Contrato de API necessário" section of `refinements/frontend/cadastro-despesa-recorrente.md` to match the same versioned route and envelope (FR-016)
- [X] T036 Execute `quickstart.md`'s 5 manual scenarios end-to-end against a locally running `dotnet run` instance, and confirm `dotnet test backend/Api.Tests/ContasEmDia.Api.Tests.csproj` passes in full

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup (T001/T002 for T009/T010) — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational completion — delivers the base controller/success path (MVP)
- **User Story 2 (Phase 4)**: Depends on User Story 1 (extends the same controller action and response mapping)
- **User Story 3 (Phase 5)**: Depends on User Story 1 (extends `Program.cs`); independent of Phase 4's controller/mapping change; shares `Program.cs` sequentially with Phase 6 (T032)
- **User Story 4 (Phase 6)**: Depends on User Story 1 (controller must exist to document); independent of Phases 4/5's content, but shares `Program.cs`
- **Polish (Phase 7)**: Depends on all four User Stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: No dependency on other stories — delivers the base endpoint
- **User Story 2 (P2)**: Builds on User Story 1's controller/mapping files — not independently implementable in isolation, but independently *testable* per its Independent Test criterion
- **User Story 3 (P3)**: Builds on User Story 1's `Program.cs`, independent of User Story 2's content
- **User Story 4 (P4)**: Builds on User Story 1's controller, independent of User Story 2/3's content

### Within User Story 1

- T014–T017 (Request/Response DTOs) before T018/T019 (mappings, which reference them)
- T018, T019 (mappings) before T020 (controller, which references them)
- T011–T013 (tests, must fail first) before T020 (implementation)

### Parallel Opportunities

- T001, T002 (Setup) in parallel
- T004, T005, T006, T008 (Phase 2) in parallel — 4 different files, no cross-dependency until T009
- T014, T015, T016, T017 (Phase 3 DTOs) in parallel — 4 different files
- T018, T019 (Phase 3 mappings) in parallel — different files, each depending only on the DTOs above
- T031, T032 (Phase 6) in parallel — different files
- T033, T034, T035 (Polish) in parallel — different files

---

## Parallel Example: User Story 1 DTOs

```bash
# Launch all four Request/Response DTOs together (different files):
Task: "Create CreateRecurringExpenseDataRequest record in backend/Api/Requests/CreateRecurringExpenseDataRequest.cs"
Task: "Create ReferencePeriodDataResponse record in backend/Api/Responses/ReferencePeriodDataResponse.cs"
Task: "Create OccurrenceDataResponse record in backend/Api/Responses/OccurrenceDataResponse.cs"
Task: "Create CreateRecurringExpenseDataResponse record in backend/Api/Responses/CreateRecurringExpenseDataResponse.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — envelope types, `SystemCurrentDateProvider`, exception middleware, DI composition, test fixture)
3. Complete Phase 3: User Story 1 — the endpoint accepts and persists a valid registration end-to-end
4. **STOP and VALIDATE**: Run `Api.Tests`, confirm the success scenarios pass

### Incremental Delivery

1. Setup + Foundational → foundation ready
2. User Story 1 → success path implemented and tested (MVP)
3. User Story 2 → business-rule failure envelope locked in by tests
4. User Story 3 → shape/presence failure envelope locked in by tests, unified with User Story 2's format
5. User Story 4 → OpenAPI/SwaggerUI documentation of the complete contract
6. Polish → `500` edge case, FR-016 contract-doc updates, quickstart verification

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- This feature has exactly one controller action; User Stories 2–4 extend files User Story 1 creates by design — see the Organization note at the top
- Commit after each task or logical group
- No mocking library is used anywhere in this repository — any test double needed for T033 should be hand-written, matching the pattern already used by `Application.Tests`/`Domain.Tests`
