---

description: "Task list template for feature implementation"
---

# Tasks: Painel Mensal de Despesas

**Input**: Design documents from `/specs/006-painel-mensal-despesas/`
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/api-contract.md](./contracts/api-contract.md), [quickstart.md](./quickstart.md)

**Design de referência**: `design/Main.dc.html` — toda tarefa de frontend com marcação/estilo deve reproduzir fielmente esse arquivo (cores, espaçamento, tipografia, layout), com o único desvio documentado registrado em `research.md` §8 (o link "Desfazer" vira um `<button>` real, em vez do `<span onClick>` do protótipo, por exigência de acessibilidade — Princípio II).

**Tests**: Incluídos e OBRIGATÓRIOS (não opcionais) — a Constitution Check de `plan.md` (Princípio II — Test-First Development) exige um teste por cenário de resposta declarado via `ProducesResponseType` no backend, e cobertura Vitest das regras de negócio client-side no frontend. Escreva cada teste antes da implementação correspondente e confirme que falha antes de implementar.

**Organization**: Tarefas agrupadas por user story (spec.md) para permitir implementação e teste independentes de cada uma.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependência de tarefas incompletas)
- **[Story]**: US1 (P1 — visualizar painel), US2 (P2 — marcar como paga), US3 (P3 — desfazer pagamento)
- Caminhos de arquivo exatos em cada descrição

## Path Conventions

Projeto web já existente (não um projeto novo): `backend/{Domain,Domain.Tests,Application,Application.Tests,Infrastructure,Infrastructure.Tests,Api,Api.Tests}/` (solution `backend/ContasEmDia.sln`) e `frontend/src/app/` (workspace Angular já existente). Nenhum projeto novo é criado — apenas arquivos novos/modificados dentro dos projetos já existentes.

---

## Phase 1: Setup

**Purpose**: Confirmar que o ambiente está pronto — nenhuma dependência nova é introduzida por esta feature (ver `plan.md` "Primary Dependencies").

- [X] T001 Confirmar branch `006-painel-mensal-despesas` ativa; rodar `dotnet restore backend/ContasEmDia.sln` e `npm install` em `frontend/` para garantir que o ambiente compila antes de qualquer alteração (nenhum pacote novo é esperado — `@angular/router` já está em `frontend/package.json`, apenas ainda não utilizado)

**Checkpoint**: Build limpo confirmado nas duas stacks antes de iniciar mudanças.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Infraestrutura de domínio compartilhada por TODAS as user stories — o rastreamento de pagamento em `Occurrence` e o status derivado são lidos tanto pela listagem (US1) quanto pelas respostas de marcar/desfazer pagamento (US2/US3); a coluna de banco é a mesma para os três fluxos.

**⚠️ CRITICAL**: Nenhuma user story pode começar antes desta fase estar completa.

- [X] T002 [P] Criar `OccurrenceDerivedStatus` (Value Object) em `backend/Domain/ValueObjects/OccurrenceDerivedStatus.cs` — `enum OccurrenceDerivedStatusType { Paid, Overdue, DueSoon, Pending }` + classe wrapper com construtor validando `Enum.IsDefined` (lança `ArgumentException` com mensagem PT-BR se inválido) e `GetValue()`, seguindo exatamente o padrão de `backend/Domain/ValueObjects/OccurrenceStatus.cs`
- [X] T003 [P] Criar `backend/Domain.Tests/ValueObjects/OccurrenceDerivedStatusTests.cs` cobrindo construção válida para cada valor do enum e `ArgumentException` para valor fora do enum (escrever antes de T002 passar — TDD)
- [X] T004 Adicionar campos privados `_paidAmount: Money?` e `_paymentDate: CalendarDate?`, atualizar o construtor privado de reconstrução (uso exclusivo do EF Core) para recebê-los, e adicionar `GetPaidAmount(): Money?`, `GetPaymentDate(): CalendarDate?` e `GetDerivedStatus(DateOnly referenceDate): OccurrenceDerivedStatus` (prioridade: `Paid` se `_status == Paid`; senão `Overdue` se `_dueDate < referenceDate`; senão `DueSoon` se a diferença em dias for `<= 7` — 0 a 7 inclusive; senão `Pending`) em `backend/Domain/Entities/Occurrence.cs`
- [X] T005 Criar `backend/Domain.Tests/Entities/OccurrenceTests.cs` com testes de `GetDerivedStatus` cobrindo EC03–EC06 (ocorrência paga é sempre "Paga" independente da data; vencimento no passado → `Overdue`; vencimento hoje → `DueSoon`; vencimento em exatamente 7 dias → `DueSoon`; vencimento em 8 dias → `Pending`; vencimento próximo da virada de mês usando a data completa, não o dia isolado) e de `GetPaidAmount()`/`GetPaymentDate()` retornando `null` enquanto pendente (depende de T004 existir para compilar; escrever os casos antes de confirmar que passam)
- [X] T006 [P] Adicionar mapeamento das colunas novas `PaidAmount` (`decimal(18,2)`, nullable, `Money?` ↔ `decimal?`) e `PaymentDate` (`date`, nullable, `CalendarDate?` ↔ `DateOnly?`) em `backend/Infrastructure/Configs/OccurrenceConfigurations.cs`
- [X] T007 Gerar a migration EF Core aditiva `AddOccurrencePaymentTracking` (a partir de `backend/Infrastructure`, apontando `backend/Api` como startup project) adicionando as duas colunas novas à tabela `Occurrences` já existente — nenhuma migration existente é alterada (depende de T004, T006)
- [X] T008 [P] Generalizar `options.InvalidModelStateResponseFactory` em `backend/Api/Program.cs` (linha ~25–33) de `ApiResponse<CreateRecurringExpenseDataResponse>` para `ApiResponse<object>` (research.md §6 — correção necessária para que o novo endpoint `GET /api/v1/occurrences`, sem `DataRequest` de corpo, também seja coberto corretamente por essa fábrica)

**Checkpoint**: `Occurrence` com estado de pagamento e status derivado testável; schema de banco pronto; user stories podem começar.

---

## Phase 3: User Story 1 - Visualizar o painel mensal de uma competência (Priority: P1) 🎯 MVP

**Goal**: Abrir o painel e ver, para uma competência (mês/ano), o cabeçalho, os dois banners condicionais, os três cartões de resumo e a lista completa de ocorrências com status derivado — sem nenhuma ação de pagamento.

**Independent Test**: Carregar o painel para uma competência com ocorrências conhecidas (pagas, vencidas, vencendo em breve, pendentes) e verificar que cabeçalho, banners, cartões e lista batem com os dados esperados (spec.md, User Story 1, cenários 1–7).

### Tests for User Story 1 (escrever antes da implementação — TDD)

- [X] T009 [P] [US1] Adicionar testes de `RecurringExpense.GetOccurrencesForPeriod` (filtra apenas ocorrências cujo `ReferencePeriod` bate ano/mês; competência sem nenhuma ocorrência retorna vazio) em `backend/Domain.Tests/Aggregates/RecurringExpenseTests.cs`
- [X] T010 [P] [US1] Adicionar testes de `RecurringExpenseRepository.GetByReferencePeriodAsync` (retorna despesas ativas E pausadas com ao menos uma ocorrência no período; competência sem ocorrências retorna coleção vazia) em `backend/Infrastructure.Tests/Repositories/RecurringExpenseRepositoryTests.cs`
- [X] T011 [P] [US1] Criar `backend/Application.Tests/UseCases/GetMonthlyPanel/GetMonthlyPanelUseCaseTests.cs` cobrindo: sem `year`/`month` → competência atual (FR-002); apenas um dos dois informado → `FieldError("period", "Informe mês e ano juntos, ou nenhum dos dois.")`; não numérico → `FieldError("period", "Mês e ano devem ser números válidos.")`; mês fora de 1–12 → `FieldError("period", ex.Message)`; sucesso mapeando cada ocorrência com `GetDerivedStatus`; competência sem ocorrências → lista vazia (EC01)
- [X] T012 [P] [US1] Criar `backend/Api.Tests/Controllers/OccurrencesControllerTests.cs` com os cenários do `GET` declarados em `ProducesResponseType` (200 com dados, 200 lista vazia, 400 para os três casos de EC16/CA14 — mês fora de 1–12, ano/mês não numérico, apenas um informado, 500 genérico)

### Implementation for User Story 1

- [X] T013 [US1] Adicionar `GetOccurrencesForPeriod(ReferencePeriod referencePeriod)` a `backend/Domain/Aggregates/RecurringExpense.cs`, filtrando `_occurrences` pelo `ReferencePeriod` (depende de T004, T009)
- [X] T014 [US1] Adicionar `GetByReferencePeriodAsync(ReferencePeriod referencePeriod): Task<IReadOnlyCollection<RecurringExpense>>` a `backend/Domain/Repositories/IRecurringExpenseRepository.cs`
- [X] T015 [US1] Implementar `GetByReferencePeriodAsync` em `backend/Infrastructure/Repositories/RecurringExpenseRepository.cs` (retorna todo agregado, ativo ou pausado, com ao menos uma ocorrência na competência pedida — projeção SQL direta ou `Include` + filtro em memória, conforme viabilidade de tradução do EF Core para `ReferencePeriod` como `ComplexProperty`, ver research.md §3) (depende de T014, T010)
- [X] T016 [P] [US1] Criar `GetMonthlyPanelUseCaseInput { string? Year, string? Month }` e `GetMonthlyPanelUseCaseOutput { bool IsSuccess; int? Year; int? Month; IReadOnlyCollection<PanelOccurrenceData>? Occurrences; IReadOnlyCollection<FieldError> Errors }` (incluindo o record `PanelOccurrenceData`) em `backend/Application/UseCases/GetMonthlyPanel/`
- [X] T017 [US1] Implementar `IGetMonthlyPanelUseCase`/`GetMonthlyPanelUseCase` em `backend/Application/UseCases/GetMonthlyPanel/` seguindo a lógica de `data-model.md` (resolução de competência atual via `ICurrentDateProvider`, validação de período, busca via `IRepositoryManager.RecurringExpenseRepository`, montagem de `PanelOccurrenceData` com `GetDerivedStatus(hoje)`) (depende de T013, T015, T016, T011)
- [X] T018 [P] [US1] Criar `GetMonthlyPanelDataResponse { ReferencePeriodDataResponse ReferencePeriod, IReadOnlyCollection<PanelOccurrenceDataResponse> Occurrences }` e `PanelOccurrenceDataResponse { Guid Id, string Name, string Category, decimal ExpectedAmount, DateOnly DueDate, string Status, decimal? PaidAmount, DateOnly? PaymentDate }` em `backend/Api/Responses/`
- [X] T019 [US1] Criar `GetMonthlyPanelDataResponseMapping` em `backend/Api/Mappings/` mapeando `GetMonthlyPanelUseCaseOutput` → `GetMonthlyPanelDataResponse` (depende de T016, T018)
- [X] T020 [US1] Criar `backend/Api/Controllers/OccurrencesController.cs` com a rota `api/v1/occurrences` e a ação `GET` (query `year`/`month` opcionais, `ProducesResponseType` 200/400/500, envelope `ApiResponse<GetMonthlyPanelDataResponse>`) (depende de T017, T019, T012)
- [X] T021 [US1] Registrar `IGetMonthlyPanelUseCase`/`GetMonthlyPanelUseCase` em `builder.Services` de `backend/Api/Program.cs` (mesmo padrão de `ICreateRecurringExpenseUseCase`) (depende de T017)
- [X] T022 [P] [US1] Criar `frontend/src/app/features/painel-mensal-despesas/painel-mensal-despesas.model.ts` — tipo `PanelOccurrenceResponse` (espelha `PanelOccurrenceDataResponse`), mapa de rótulo/cor por status derivado (`Paid`→"Paga" `#E7F6EF`/`#0F7B4E`; `Overdue`→"Vencida" `#FDECEA`/`#C0362C`; `DueSoon`→"Vence em breve" `#FFF6E5`/`#9A6300`; `Pending`→"Pendente" `#EEF2FF`/`#3446C9`), reaproveitando `CATEGORY_COLORS`/`CATEGORY_OPTIONS` de `frontend/src/app/features/despesa-recorrente/despesa-recorrente.model.ts` sem duplicação
- [X] T023 [US1] Criar `frontend/src/app/features/painel-mensal-despesas/painel-mensal-despesas.service.ts` — serviço `providedIn: 'root'` com `HttpClient` via `inject()`, método `getMonthlyPanel(year?: string, month?: string)` chamando `GET /api/v1/occurrences` (depende de T022)
- [X] T024 [P] [US1] Criar `frontend/src/app/features/painel-mensal-despesas/painel-mensal-despesas.service.spec.ts` com `HttpTestingController` cobrindo chamada sem parâmetros e com `year`/`month` (depende de T023)
- [X] T025 [US1] Criar `PainelMensalDespesasComponent` standalone (`painel-mensal-despesas.component.ts` + `.html`) reproduzindo fielmente `design/Main.dc.html`: cabeçalho com ícone/nome do app, navegação de mês (setas sem `onClick` — fora de escopo, research.md §7) e "Nova despesa" (sem `onClick`), os dois banners condicionais (`hasVencidas`/`hasVenceEmBreve`), os três cartões de resumo, título "Contas de {mês} {ano}" com contador, e a lista de ocorrências (indicador de cor por categoria, nome, categoria, valor previsto formatado em reais, "Dia N", selo de status) com o botão "Marcar como paga" visível para ocorrências não pagas (ação plugada em US2) e o bloco "pago em {data}"/aviso "diferente do previsto" para ocorrências pagas; estado em `signal` (`occurrences`, `referencePeriod`) e `computed` (`totalPrevisto`, `totalPago`, `totalPendente`, `vencidasCount`, `venceEmBreveCount`, `totalAVencer`, itens de exibição com rótulo/cor de status) — mesma fórmula de `renderVals()` do protótipo, agora sobre dados da API; carrega via `painel-mensal-despesas.service.ts` no `ngOnInit`/`constructor` (depende de T023, T022)
- [X] T026 [P] [US1] Criar `painel-mensal-despesas.component.spec.ts` cobrindo: selo de status por ocorrência (Paga/Vencida/Vence em breve/Pendente), banners aparecendo/somem conforme `hasVencidas`/`hasVenceEmBreve`, os três totais somando corretamente, estado vazio (0 contas, R$ 0,00 nos três cartões, nenhum banner — EC01/SC-005) (depende de T025)
- [X] T027 [US1] Criar `frontend/src/app/app.routes.ts` com `{ path: '', component: PainelMensalDespesasComponent }` e `{ path: 'despesas/nova', component: CadastroDespesaRecorrenteComponent }` (depende de T025)
- [X] T028 [US1] Atualizar `frontend/src/app/app.config.ts` para incluir `provideRouter(routes)` (depende de T027)
- [X] T029 [US1] Atualizar `frontend/src/app/app.html` para `<router-outlet />` e `frontend/src/app/app.ts` para remover a montagem fixa de `CadastroDespesaRecorrenteComponent` (depende de T027)
- [X] T030 [US1] Atualizar `frontend/src/app/app.spec.ts` para refletir a composição via router (depende de T029)

**Checkpoint**: User Story 1 completa e testável de forma independente — painel visível em `/`, cadastro ainda acessível em `/despesas/nova`, sem nenhuma ação de pagamento funcional ainda.

---

## Phase 4: User Story 2 - Marcar uma ocorrência como paga (Priority: P2)

**Goal**: A partir do painel (US1), iniciar o registro de pagamento de uma ocorrência não paga, ajustar (ou manter) valor/data sugeridos e confirmar — a ocorrência passa a "paga".

**Independent Test**: Painel com ao menos uma ocorrência não paga; iniciar pagamento, confirmar com valor/data válidos e depois com valor/data em branco/inválidos; verificar que a ocorrência fica "paga" com os dados corretos em cada caso (spec.md, User Story 2, cenários 1–7).

### Tests for User Story 2 (escrever antes da implementação — TDD)

- [X] T031 [P] [US2] Adicionar a `backend/Domain.Tests/Entities/OccurrenceTests.cs` testes de `MarkAsPaid` (sucesso: `_status`/`_paidAmount`/`_paymentDate` atualizados; lança `DomainRuleViolationException("Esta ocorrência já está paga.")` se já `Paid`)
- [X] T032 [P] [US2] Adicionar a `backend/Domain.Tests/Aggregates/RecurringExpenseTests.cs` testes de `FindOccurrence` (encontrada/`null`) e `MarkOccurrenceAsPaid` (delega corretamente; lança `KeyNotFoundException("Ocorrência não encontrada.")` para id inexistente)
- [X] T033 [P] [US2] Adicionar a `backend/Infrastructure.Tests/Repositories/RecurringExpenseRepositoryTests.cs` testes de `GetByOccurrenceIdAsync` (encontrado/`null`) e `UpdateAsync` (persiste mutação do agregado já tracked)
- [X] T034 [P] [US2] Criar `backend/Application.Tests/UseCases/MarkOccurrenceAsPaid/MarkOccurrenceAsPaidUseCaseTests.cs` cobrindo: valor/data válidos; valor vazio ou não numérico → usa `ExpectedAmount` (EC08); data vazia → usa data atual do `ICurrentDateProvider` (EC09); `occurrenceId` inexistente → `KeyNotFoundException`; ocorrência já paga → `DomainRuleViolationException`
- [X] T035 [P] [US2] Adicionar a `backend/Api.Tests/Controllers/OccurrencesControllerTests.cs` os cenários do `PATCH .../payment` (200 sucesso, 400 regra de negócio, 404 não encontrado, 500 genérico)
- [X] T036 [P] [US2] Adicionar a `backend/Api.Tests/ExceptionHandlingMiddlewareTests.cs` os cenários novos — `DomainRuleViolationException` → 400, `KeyNotFoundException` → 404 — sem quebrar o teste existente que espera `InvalidOperationException` de persistência mapeando para 500

### Implementation for User Story 2

- [X] T037 [P] [US2] Criar `backend/Domain/DomainRuleViolationException.cs` — `sealed class DomainRuleViolationException : Exception` com construtor `(string message)`
- [X] T038 [US2] Adicionar `MarkAsPaid(Money paidAmount, CalendarDate paymentDate)` a `backend/Domain/Entities/Occurrence.cs` (depende de T004, T037, T031)
- [X] T039 [US2] Adicionar `FindOccurrence(Guid occurrenceId): Occurrence?` e `MarkOccurrenceAsPaid(Guid occurrenceId, Money paidAmount, CalendarDate paymentDate)` a `backend/Domain/Aggregates/RecurringExpense.cs` (localiza via `FindOccurrence`; lança `KeyNotFoundException("Ocorrência não encontrada.")` se não existir; senão delega a `Occurrence.MarkAsPaid`) (depende de T038, T032)
- [X] T040 [US2] Adicionar `GetByOccurrenceIdAsync(Guid occurrenceId): Task<RecurringExpense?>` e `UpdateAsync(RecurringExpense recurringExpense): Task` (chama apenas `SaveChangesAsync()`, mesmo padrão de `AddAsync`) a `backend/Domain/Repositories/IRecurringExpenseRepository.cs` e implementar em `backend/Infrastructure/Repositories/RecurringExpenseRepository.cs` (depende de T033)
- [X] T041 [US2] Estender `backend/Api/Middlewares/ExceptionHandlingMiddleware.cs` com dois `catch` novos antes do `catch (Exception)` genérico: `DomainRuleViolationException` → `400 Bad Request`, `KeyNotFoundException` → `404 Not Found`, ambos reaproveitando `ApiResponse<object>.Failure([...])` com a mensagem da exceção relayed as-is (depende de T037, T036)
- [X] T042 [P] [US2] Criar `MarkOccurrenceAsPaidUseCaseInput { Guid OccurrenceId, string? PaidAmountRaw, string? PaymentDateRaw }` e `MarkOccurrenceAsPaidUseCaseOutput { PanelOccurrenceData Occurrence }` em `backend/Application/UseCases/MarkOccurrenceAsPaid/`
- [X] T043 [US2] Implementar `IMarkOccurrenceAsPaidUseCase`/`MarkOccurrenceAsPaidUseCase` em `backend/Application/UseCases/MarkOccurrenceAsPaid/` (busca via `GetByOccurrenceIdAsync`; parse de valor com vírgula decimal e fallback para `GetExpectedAmount()`; parse de data `dd/MM/yyyy` com fallback para `ICurrentDateProvider.GetCurrentDate()`; chama `MarkOccurrenceAsPaid` no agregado; `UpdateAsync`; retorna `PanelOccurrenceData` atualizado) (depende de T039, T040, T042, T034)
- [X] T044 [P] [US2] Criar `MarkOccurrenceAsPaidDataRequest { string? PaidAmount, string? PaymentDate }` (sem `[Required]` — FR-013) em `backend/Api/Requests/` e `MarkOccurrenceAsPaidDataResponse { PanelOccurrenceDataResponse Occurrence }` em `backend/Api/Responses/`
- [X] T045 [US2] Criar `MarkOccurrenceAsPaidDataRequestMapping` e `MarkOccurrenceAsPaidDataResponseMapping` em `backend/Api/Mappings/` (depende de T044, T018)
- [X] T046 [US2] Adicionar a ação `PATCH {occurrenceId}/payment` a `backend/Api/Controllers/OccurrencesController.cs` (`ProducesResponseType` 200/400/404/500, envelope `ApiResponse<MarkOccurrenceAsPaidDataResponse>`) (depende de T043, T045, T041, T035, T020)
- [X] T047 [US2] Registrar `IMarkOccurrenceAsPaidUseCase`/`MarkOccurrenceAsPaidUseCase` em `backend/Api/Program.cs` (depende de T043)
- [X] T048 [US2] Estender `painel-mensal-despesas.service.ts` com `markOccurrenceAsPaid(occurrenceId: string, paidAmount: string, paymentDate: string)` chamando `PATCH /api/v1/occurrences/{occurrenceId}/payment` (depende de T023)
- [X] T049 [US2] Estender `PainelMensalDespesasComponent` (`.ts`/`.html`) com `signal`s `editingOccurrenceId`, `draftValor`, `draftData`; `iniciarPagamento(id)` (pré-preenche valor previsto e data de hoje, cancela automaticamente qualquer edição em andamento — FR-011/FR-012); `confirmarPagamento(id)` (chama o serviço, atualiza a ocorrência no signal `occurrences`, sai do modo de edição); `cancelarEdicao()` (sai da edição sem chamar o serviço); formulário inline de edição (campos "R$" e data, botões "Confirmar"/"Cancelar") reproduzindo fielmente o bloco `sc-if value="{{it.isEditing}}"` de `design/Main.dc.html` (depende de T048, T025)
- [X] T050 [P] [US2] Estender `painel-mensal-despesas.component.spec.ts` com: iniciar edição pré-preenche valor/data; iniciar edição de uma segunda ocorrência cancela a primeira sem chamada de rede associada a ela (RF14/EC10); confirmar com valor/data válidos; confirmar com valor vazio/inválido; confirmar com data vazia; cancelar não altera dados; ocorrência com valor pago diferente do previsto exibe aviso "diferente do previsto", igual não exibe (depende de T049)
- [X] T051 [P] [US2] Estender `painel-mensal-despesas.service.spec.ts` com o formato da chamada `PATCH` (depende de T048)

**Checkpoint**: User Story 2 completa — marcar pagamento funcional de ponta a ponta, sem regredir User Story 1.

---

## Phase 5: User Story 3 - Desfazer o pagamento de uma ocorrência (Priority: P3)

**Goal**: A partir de uma ocorrência paga, desfazer o pagamento — ela volta a "não paga", elegível para novo registro de pagamento.

**Independent Test**: Painel com ao menos uma ocorrência paga; clicar "Desfazer"; verificar que volta a "não paga", sem valor pago nem data de pagamento, com "Marcar como paga" disponível novamente (spec.md, User Story 3, cenários 1–2).

### Tests for User Story 3 (escrever antes da implementação — TDD)

- [X] T052 [P] [US3] Adicionar a `backend/Domain.Tests/Entities/OccurrenceTests.cs` testes de `UndoPayment` (sucesso: `_status`/`_paidAmount`/`_paymentDate` revertidos; lança `DomainRuleViolationException("Esta ocorrência ainda não foi paga.")` se já `Pending`)
- [X] T053 [P] [US3] Adicionar a `backend/Domain.Tests/Aggregates/RecurringExpenseTests.cs` testes de `UndoOccurrencePayment` (delega corretamente; `KeyNotFoundException` para id inexistente)
- [X] T054 [P] [US3] Criar `backend/Application.Tests/UseCases/UndoOccurrencePayment/UndoOccurrencePaymentUseCaseTests.cs` cobrindo: sucesso com status recalculado a partir de `dueDate` (pode voltar a `Overdue`/`DueSoon`/`Pending`); `occurrenceId` inexistente → `KeyNotFoundException`; ocorrência não paga → `DomainRuleViolationException`
- [X] T055 [P] [US3] Adicionar a `backend/Api.Tests/Controllers/OccurrencesControllerTests.cs` os cenários do `DELETE .../payment` (200 sucesso com status recalculado, 400 regra de negócio, 404 não encontrado, 500 genérico)

### Implementation for User Story 3

- [X] T056 [US3] Adicionar `UndoPayment()` a `backend/Domain/Entities/Occurrence.cs` (depende de T004, T052)
- [X] T057 [US3] Adicionar `UndoOccurrencePayment(Guid occurrenceId)` a `backend/Domain/Aggregates/RecurringExpense.cs` (reutiliza `FindOccurrence`; delega a `Occurrence.UndoPayment`) (depende de T056, T039, T053)
- [X] T058 [P] [US3] Criar `UndoOccurrencePaymentUseCaseInput { Guid OccurrenceId }` e `UndoOccurrencePaymentUseCaseOutput { PanelOccurrenceData Occurrence }` em `backend/Application/UseCases/UndoOccurrencePayment/`
- [X] T059 [US3] Implementar `IUndoOccurrencePaymentUseCase`/`UndoOccurrencePaymentUseCase` (busca via `GetByOccurrenceIdAsync`; chama `UndoOccurrencePayment` no agregado; `UpdateAsync`; retorna `PanelOccurrenceData` com status recalculado) (depende de T057, T040, T058, T054)
- [X] T060 [P] [US3] Criar `UndoOccurrencePaymentDataResponse { PanelOccurrenceDataResponse Occurrence }` em `backend/Api/Responses/` e `UndoOccurrencePaymentDataResponseMapping` em `backend/Api/Mappings/`
- [X] T061 [US3] Adicionar a ação `DELETE {occurrenceId}/payment` a `backend/Api/Controllers/OccurrencesController.cs` (`ProducesResponseType` 200/400/404/500, envelope `ApiResponse<UndoOccurrencePaymentDataResponse>`) (depende de T059, T060, T055, T046)
- [X] T062 [US3] Registrar `IUndoOccurrencePaymentUseCase`/`UndoOccurrencePaymentUseCase` em `backend/Api/Program.cs` (depende de T059)
- [X] T063 [US3] Estender `painel-mensal-despesas.service.ts` com `undoOccurrencePayment(occurrenceId: string)` chamando `DELETE /api/v1/occurrences/{occurrenceId}/payment` (depende de T048)
- [X] T064 [US3] Estender `PainelMensalDespesasComponent` com `desfazerPagamento(id)` (chama o serviço imediatamente, sem confirmação adicional, atualiza a ocorrência no signal `occurrences`); implementar a ação "Desfazer" como `<button type="button">` com a mesma aparência do `cd-link` do design (não um `<span onClick>`) para garantir operabilidade via teclado — desvio de fidelidade literal ao markup documentado em `research.md` §8, mantendo fidelidade visual (depende de T063, T049)
- [X] T065 [P] [US3] Estender `painel-mensal-despesas.component.spec.ts` com: desfazer pagamento reverte para "não paga" sem valor/data; status recalculado após desfazer (pode virar Vencida/Vence em breve/Pendente); botão "Desfazer" é operável via teclado (foco + Enter/Espaço aciona `desfazerPagamento`) (depende de T064)
- [X] T066 [P] [US3] Estender `painel-mensal-despesas.service.spec.ts` com o formato da chamada `DELETE` (depende de T063)

**Checkpoint**: Todas as três user stories funcionais de ponta a ponta — painel completo conforme `design/Main.dc.html`.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validação final de ponta a ponta e conformidade com o design/contrato, sem introduzir comportamento novo.

- [X] T067 [P] Rodar `dotnet test` (backend) e `npm test` (frontend) completos e confirmar 100% verde, incluindo todos os testes novos das Phases 2–5
- [X] T068 [P] Executar os 9 cenários de `quickstart.md` (curl para os três endpoints + fluxo de edição exclusiva na UI) e confirmar que cada resultado esperado bate exatamente com o documentado
- [X] T069 [P] Revisão visual pixel-a-pixel de `PainelMensalDespesasComponent` contra `design/Main.dc.html` (cores, espaçamento, tipografia Sora/Public Sans, tamanhos, estados de banner/cartão/lista/edição) no navegador com o backend rodando
- [X] T070 [P] Revisão de acessibilidade WCAG 2.1 AA do componente novo (labels dos campos de valor/data na edição, operabilidade via teclado de todos os controles interativos incluindo "Desfazer", contraste dos selos de status e banners)
- [X] T071 Conferir que `Swagger`/OpenAPI (`Swashbuckle.AspNetCore`) documenta os três endpoints novos com todos os `ProducesResponseType` esperados por ação, conforme `contracts/api-contract.md`

---

## Phase 7: Navegação de mês e botão "Nova despesa" (FR-020/FR-021)

**Purpose**: Fechar a lacuna aberta pela clarificação de `spec.md` (sessão 2026-09-07): as setas de navegação de mês e o botão "Nova despesa" deixam de ficar sem `onClick` (decisão original de T025/`research.md` §7, hoje superada) e passam a ser controles funcionais, conforme `plan.md`/`research.md` §7/`data-model.md` já atualizados. Nenhum endpoint, rota ou dependência nova é introduzida — esta fase reaproveita o `GET /api/v1/occurrences` já existente (FR-002) e o `@angular/router` já introduzido em T027–T029.

**Goal**: Ao clicar na seta "mês anterior"/"próximo mês", o painel recarrega para a competência vizinha (FR-020); ao clicar em "Nova despesa", o usuário é levado para `/despesas/nova` (FR-021).

**Independent Test**: Com o painel carregado, clicar "próximo mês" e depois "mês anterior" e confirmar que a competência exibida (cabeçalho, lista, totais) volta exatamente ao ponto de partida a cada clique (`quickstart.md`, Cenário 10); clicar "Nova despesa" e confirmar que a URL muda para `/despesas/nova` e a tela de cadastro já existente aparece.

### Tests for Phase 7 (escrever antes da implementação — TDD)

- [X] T072 [P] [US1] Adicionar a `frontend/src/app/features/painel-mensal-despesas/painel-mensal-despesas.component.spec.ts` testes de `mesAnterior()`/`proximoMes()`: cada clique dispara nova chamada a `painel-mensal-despesas.service.ts` com o `year`/`month` da competência vizinha (mês anterior e próximo mês dentro do mesmo ano; janeiro → dezembro do ano anterior; dezembro → janeiro do ano seguinte)
- [X] T073 [P] [US1] Adicionar ao mesmo arquivo um teste confirmando que o botão "Nova despesa" está associado a `routerLink="/despesas/nova"` (mesmo padrão de teste de roteamento já usado em `frontend/src/app/app.spec.ts`)

### Implementation for Phase 7

- [X] T074 [US1] Adicionar `mesAnterior()`/`proximoMes()` a `painel-mensal-despesas.component.ts`: calculam a competência de destino a partir do signal `referencePeriod()` (mês -1/+1, com virada de ano), atualizam `referencePeriod` e chamam `painel-mensal-despesas.service.ts` novamente para recarregar `occurrences` (depende de T072)
- [X] T075 [US1] Conectar as duas setas de navegação em `painel-mensal-despesas.component.html` a `(click)="mesAnterior()"`/`(click)="proximoMes()"`, mantendo exatamente a aparência já implementada em T025 (nenhuma mudança de marcação/estilo, apenas o binding de clique) (depende de T074)
- [X] T076 [US1] Adicionar `RouterLink` aos `imports` standalone de `PainelMensalDespesasComponent` e aplicar `routerLink="/despesas/nova"` ao botão "Nova despesa" em `painel-mensal-despesas.component.html`, sem alterar sua aparência visual (depende de T073, T027)
- [X] T077 [US1] Executar o Cenário 10 de `quickstart.md` manualmente (navegação de mês ida e volta + clique em "Nova despesa") e confirmar que o resultado bate exatamente com o documentado (depende de T075, T076)

**Checkpoint**: Painel com navegação de mês e acesso a "Nova despesa" totalmente funcionais, sem regressão em US1/US2/US3 nem na tela de cadastro já existente.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências — pode começar imediatamente
- **Foundational (Phase 2)**: depende de Setup — BLOQUEIA todas as user stories (T002–T008)
- **User Story 1 (Phase 3)**: depende de Foundational — sem dependência de outras stories
- **User Story 2 (Phase 4)**: depende de Foundational; reutiliza `PainelMensalDespesasComponent`/serviço criados em US1 (mesmos arquivos, editados de forma incremental) — logicamente depende de US1 estar completa antes de começar, já que estende o mesmo componente e a mesma tabela de ações
- **User Story 3 (Phase 5)**: depende de Foundational; reutiliza `FindOccurrence`/`GetByOccurrenceIdAsync`/`UpdateAsync`/middleware introduzidos em US2 — depende de US2 estar completa
- **Polish (Phase 6)**: depende de todas as user stories completas
- **Fase 7 — Navegação de mês / "Nova despesa" (FR-020/FR-021)**: depende de User Story 1 (Phase 3) — estende o mesmo `PainelMensalDespesasComponent` e reutiliza a rota `despesas/nova` (T027) já criados ali; não depende de US2, US3 nem de Polish (Phase 6), podendo rodar em paralelo com elas

### User Story Dependencies

- **US1 (P1)**: independente após Foundational
- **US2 (P2)**: tecnicamente independente em termos de regra de negócio, mas reutiliza arquivos criados por US1 (`OccurrencesController`, `painel-mensal-despesas.component.ts/html`, `painel-mensal-despesas.service.ts`) — implementar após US1
- **US3 (P3)**: reutiliza `DomainRuleViolationException`, o middleware estendido e `GetByOccurrenceIdAsync`/`UpdateAsync` introduzidos em US2 — implementar após US2
- **Fase 7 (FR-020/FR-021)**: reutiliza o `PainelMensalDespesasComponent` de US1 e a rota `despesas/nova` de US1 — implementar após US1; independente de US2/US3

### Within Each User Story

- Testes escritos e falhando antes da implementação correspondente (Princípio II)
- Domain (Entity/Aggregate) → Repository → Application (UseCase) → Api (Response/Mapping/Controller/DI) → Frontend (model → service → component)
- Story completa e validada (checkpoint) antes de avançar para a próxima

### Parallel Opportunities

- T002/T003 (VO + seus testes) em paralelo com T006 (mapeamento EF Core) — arquivos diferentes
- Dentro de cada fase de user story, todas as tarefas de teste marcadas `[P]` (arquivos de teste diferentes) podem ser escritas em paralelo
- T016/T018 (records de Input/Output e de Response) em paralelo entre si
- T042/T044, T058/T060 (mesma forma nas demais stories) igualmente paralelizáveis
- Tarefas de frontend `[P]` que tocam arquivos `.spec.ts` diferentes dos arquivos de implementação podem ser preparadas em paralelo com a implementação correspondente, desde que a implementação já exista para o teste rodar
- T072/T073 (testes de navegação de mês e de `routerLink` de "Nova despesa", Fase 7) podem ser escritos em paralelo — mesmo arquivo (`painel-mensal-despesas.component.spec.ts`), mas blocos de teste independentes sem dependência entre si

---

## Parallel Example: User Story 1

```bash
# Testes de US1 em paralelo (arquivos diferentes):
Task: "RecurringExpense.GetOccurrencesForPeriod tests em backend/Domain.Tests/Aggregates/RecurringExpenseTests.cs"
Task: "RecurringExpenseRepository.GetByReferencePeriodAsync tests em backend/Infrastructure.Tests/Repositories/RecurringExpenseRepositoryTests.cs"
Task: "GetMonthlyPanelUseCase tests em backend/Application.Tests/UseCases/GetMonthlyPanel/GetMonthlyPanelUseCaseTests.cs"
Task: "OccurrencesController GET tests em backend/Api.Tests/Controllers/OccurrencesControllerTests.cs"

# Records de transporte em paralelo:
Task: "GetMonthlyPanelUseCaseInput/Output em backend/Application/UseCases/GetMonthlyPanel/"
Task: "GetMonthlyPanelDataResponse/PanelOccurrenceDataResponse em backend/Api/Responses/"
```

---

## Implementation Strategy

### MVP First (User Story 1 apenas)

1. Completar Phase 1: Setup
2. Completar Phase 2: Foundational (CRÍTICO — bloqueia todas as stories)
3. Completar Phase 3: User Story 1
4. **PARAR e VALIDAR**: testar o painel isoladamente (visualização, banners, totais, status derivado) — SC-001, SC-002, SC-005
5. Demonstrar/entregar se pronto — painel funcional em modo somente leitura

### Incremental Delivery

1. Setup + Foundational → base pronta
2. US1 → testar independentemente → demo (MVP: visualizar painel)
3. US2 → testar independentemente → demo (marcar como paga)
4. US3 → testar independentemente → demo (desfazer pagamento)
5. Polish → validação final ponta a ponta contra `design/Main.dc.html` e `quickstart.md`
6. Fase 7 → testar independentemente → demo (navegação de mês funcional + acesso a "Nova despesa", FR-020/FR-021) — pode ser feita a qualquer momento após US1, inclusive em paralelo com US2/US3/Polish

---

## Notes

- [P] = arquivos diferentes, sem dependência de tarefa incompleta
- [Story] mapeia a tarefa à user story correspondente para rastreabilidade
- Testes devem ser escritos e falhar antes da implementação (Princípio II — Test-First)
- Nenhuma migration existente é alterada — apenas uma migration aditiva nova (T007)
- O único desvio de fidelidade literal ao markup do design é o botão "Desfazer" (`<button>` em vez de `<span onClick>`), documentado em `research.md` §8 e aplicado em T064
- T025 originalmente implementou as setas de navegação de mês e o botão "Nova despesa" sem `onClick`, por decisão da spec então vigente; a clarificação de `spec.md` (sessão 2026-09-07, FR-020/FR-021) reverteu essa decisão — a Fase 7 (T072–T077) fecha essa lacuna sem reabrir ou renumerar T025
- Commit após cada tarefa ou grupo lógico de tarefas
- Parar em cada checkpoint para validar a story isoladamente antes de seguir

---

## Phase 8: Convergence

**Purpose**: `/speckit-converge` encontrou 7 lacunas entre `spec.md`/`plan.md` (FR-022 a FR-030, acrescentados em 2026-09-08) e o código atual — nenhum dos requisitos de UX dessa sessão de clarificação foi implementado ainda (confirmado por inspeção direta de `painel-mensal-despesas.component.*` e `cadastro-despesa-recorrente.component.*`: sem `cursor-pointer` na maioria dos elementos, sem breakpoints responsivos, sem máscara de moeda, campos de data ainda `type="text"`, formatação ainda em BRL/"R$", e nenhum botão/lógica de "Voltar ao painel" ou de confirmação de saída). Ver `research.md` §9–§14 e `data-model.md` ("Frontend — UX transversais acrescentadas em 2026-09-08") para as decisões técnicas que estas tarefas implementam.

### Tests for Phase 8 (escrever antes da implementação — TDD)

- [X] T078 [P] Criar `frontend/src/app/shared/currency-mask.util.spec.ts` cobrindo `maskCurrencyDigits` (agrupamento de milhar com `.`, decimal com `,`, entrada vazia, dígitos não numéricos ignorados) per FR-028 (missing)
- [X] T079 [P] Criar `frontend/src/app/shared/currency-format.util.spec.ts` cobrindo `formatEUR` (milhar `.`, decimal `,`, sufixo `" €"`, zero, valores negativos) per FR-030 (missing)
- [X] T080 [P] Estender `painel-mensal-despesas.component.spec.ts`: totais/itens exibidos via `formatEUR` (sem `"R$"`); `onDraftValorInput` aplica a máscara de moeda; `confirmarPagamento` converte o valor ISO do campo de data para `dd/MM/yyyy` antes de chamar `painelService.markOccurrenceAsPaid` per FR-028/FR-029/FR-030 (missing/contradicts)
- [X] T081 [P] Estender `cadastro-despesa-recorrente.component.spec.ts`: `valorFmt` via `formatEUR`; `onValorInput` aplica a máscara de moeda; `dataInicioError` reduzido a checagem de presença com `type="date"`; `hasUnsavedData` verdadeiro/falso (incluindo o caso: preencher e depois apagar manualmente = falso); `onClickVoltar`/`onCancelExit`/`onConfirmExit`; tela de sucesso expõe "Voltar ao painel" e "Cadastrar outra despesa" per FR-022–FR-025/FR-028–FR-030 (missing/contradicts)

### Implementation for Phase 8

- [X] T082 [P] Criar `frontend/src/app/shared/currency-mask.util.ts` exportando `maskCurrencyDigits` (depende de T078) per FR-028 (missing)
- [X] T083 [P] Criar `frontend/src/app/shared/currency-format.util.ts` exportando `formatEUR` (depende de T079) per FR-030 (missing)
- [X] T084 Substituir `formatBRL`/`Intl.NumberFormat(...BRL...)` por `formatEUR` em `painel-mensal-despesas.component.ts` (totais, itens) (depende de T083, T080) per FR-030 (contradicts)
- [X] T085 Aplicar `maskCurrencyDigits` em `onDraftValorInput`, `painel-mensal-despesas.component.ts` (depende de T082, T080) per FR-028 (missing)
- [X] T086 Trocar o campo `draftData` para `type="date"` em `painel-mensal-despesas.component.html`; adicionar conversão ISO→`dd/MM/yyyy` em `confirmarPagamento`, `painel-mensal-despesas.component.ts` (depende de T080) per FR-029 (missing)
- [X] T087 [P] Adicionar `cursor-pointer`/`disabled:cursor-default` a todo elemento clicável de `painel-mensal-despesas.component.html` per FR-022 (missing)
- [X] T088 Adicionar os breakpoints responsivos a `painel-mensal-despesas.component.html` — grade de resumo `grid-cols-3 min-[481px]:max-[720px]:grid-cols-2 max-[480px]:grid-cols-1`; colunas da linha de ocorrência `max-[720px]:order-{1..5}` (nome/status/valor/dia/ações); banners de alerta `max-[480px]:` largura total per FR-026/FR-027 (missing)
- [X] T089 Substituir `Intl.NumberFormat(...BRL...)`/label `"R$"` por `formatEUR`/`"€"` em `cadastro-despesa-recorrente.component.ts`/`.html` (depende de T083, T081) per FR-030 (contradicts)
- [X] T090 Aplicar `maskCurrencyDigits` em `onValorInput`, `cadastro-despesa-recorrente.component.ts` (depende de T082, T081) per FR-028 (missing)
- [X] T091 Trocar o campo `dataInicio` para `type="date"`; remover `parseDataInicio`/`toIsoDate`; simplificar `dataInicioError` para checagem de presença, em `cadastro-despesa-recorrente.component.ts`/`.html` (depende de T081) per FR-029 (missing)
- [X] T092 [P] Adicionar `cursor-pointer`/`disabled:cursor-default` a todo elemento clicável de `cadastro-despesa-recorrente.component.html` per FR-022 (missing)
- [X] T093 Adicionar `hasUnsavedData` (computed), `showExitConfirmDialog` (signal) e os métodos `onClickVoltar`/`onCancelExit`/`onConfirmExit` a `cadastro-despesa-recorrente.component.ts`, injetando `Router` (`@angular/router`, já dependência do projeto) (depende de T081) per FR-023/FR-024 (missing)
- [X] T094 Adicionar o botão/link "Voltar ao painel" ao cabeçalho do formulário (chamando `onClickVoltar()`) e o modal de confirmação de saída a `cadastro-despesa-recorrente.component.html` (depende de T093) per FR-023/FR-024 (missing)
- [X] T095 Adicionar o botão "Voltar ao painel" (`routerLink="/"`) ao bloco de estado de sucesso em `cadastro-despesa-recorrente.component.html`, ao lado do já existente "Cadastrar outra despesa" (depende de T093) per FR-025 (missing)
- [X] T096 [P] Adicionar `RouterLink` aos `imports` do `@Component` de `CadastroDespesaRecorrenteComponent` (depende de T093) per FR-023/FR-025 (missing)

### Polish

- [X] T097 Executar manualmente os Cenários 11–17 de `quickstart.md` (cursor de mão, "Voltar ao painel" com/sem dados não salvos, duas ações da tela de sucesso, breakpoints 720px/480px, máscara de moeda, seletor de data nativo, valores em Euro) e confirmar que cada resultado bate com o documentado per FR-022–FR-030 (missing)

**Checkpoint**: Todos os requisitos de UX acrescentados a `spec.md` em 2026-09-08 (FR-022 a FR-030) implementados e testados, sem regressão em US1/US2/US3/Fase 7.
