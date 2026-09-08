# Phase 1 Data Model: Painel Mensal de Despesas

**Feature**: `006-painel-mensal-despesas` | **Date**: 2026-09-07

Ver `research.md` para a justificativa de cada decisão abaixo. Tipos do
Domain (comportamento + invariantes) são descritos primeiro; tipos de
transporte (Application/API) depois, no mesmo espírito de
`004-api-despesa-recorrente/data-model.md`.

## Domain

### `Occurrence` (Entity, `Domain/Entities/Occurrence.cs`) — alterações

Campos privados novos (além dos já existentes `_id`, `_referencePeriod`,
`_dueDate`, `_status`, `_name`, `_category`, `_expectedAmount`):

| Campo | Tipo | Presença |
|---|---|---|
| `_paidAmount` | `Money?` | `null` enquanto `_status == Pending`; obrigatório quando `Paid` |
| `_paymentDate` | `CalendarDate?` | `null` enquanto `_status == Pending`; obrigatório quando `Paid` |

Métodos novos (todos de intenção de negócio — nenhum setter):

| Método | Efeito | Invariante |
|---|---|---|
| `MarkAsPaid(Money paidAmount, CalendarDate paymentDate)` | `_status ← Paid`; `_paidAmount ← paidAmount`; `_paymentDate ← paymentDate` | Lança `DomainRuleViolationException("Esta ocorrência já está paga.")` se já `Paid` |
| `UndoPayment()` | `_status ← Pending`; `_paidAmount ← null`; `_paymentDate ← null` | Lança `DomainRuleViolationException("Esta ocorrência ainda não foi paga.")` se já `Pending` |
| `GetDerivedStatus(DateOnly referenceDate)` | Retorna `OccurrenceDerivedStatus` computado (nunca persistido) | Ver regra abaixo |
| `GetPaidAmount()` | Retorna `_paidAmount` (`Money?`) | — |
| `GetPaymentDate()` | Retorna `_paymentDate` (`CalendarDate?`) | — |

Regra de `GetDerivedStatus` (FR-005, RF11/RF22, EC03–EC06), nesta ordem de
prioridade:

1. `_status == Paid` → `OccurrenceDerivedStatus(Paid)`.
2. `_dueDate.GetValue() < referenceDate` → `Overdue` ("Vencida").
3. `(_dueDate.GetValue().DayNumber - referenceDate.DayNumber) <= 7` → `DueSoon`
   ("Vence em breve"; inclusive nas duas pontas — vence hoje ou em exatamente
   7 dias contam).
4. Caso contrário → `Pending` ("Pendente").

O construtor de reconstrução privado (uso exclusivo do EF Core) passa a
receber também `Money? paidAmount` e `CalendarDate? paymentDate`.

### `RecurringExpense` (Aggregate Root, `Domain/Aggregates/RecurringExpense.cs`) — alterações

Métodos novos, todos operando sobre `_occurrences` (a única forma de
alcançar uma `Occurrence`, preservando a regra "Entidades só são
alcançáveis através do Agregado dono"):

| Método | Efeito |
|---|---|
| `FindOccurrence(Guid occurrenceId)` | Retorna a `Occurrence?` com esse id, ou `null` |
| `GetOccurrencesForPeriod(ReferencePeriod referencePeriod)` | Retorna as ocorrências cujo `GetReferencePeriod()` tem o mesmo `Year`/`Month` |
| `MarkOccurrenceAsPaid(Guid occurrenceId, Money paidAmount, CalendarDate paymentDate)` | Localiza a ocorrência via `FindOccurrence`; lança `KeyNotFoundException("Ocorrência não encontrada.")` se não existir; senão delega a `Occurrence.MarkAsPaid` |
| `UndoOccurrencePayment(Guid occurrenceId)` | Mesma busca; delega a `Occurrence.UndoPayment` |

### `OccurrenceDerivedStatus` (Value Object novo, `Domain/ValueObjects/OccurrenceDerivedStatus.cs`)

```text
enum OccurrenceDerivedStatusType { Paid, Overdue, DueSoon, Pending }

class OccurrenceDerivedStatus
  ctor(OccurrenceDerivedStatusType value)   — lança ArgumentException PT-BR se !Enum.IsDefined
  GetValue(): OccurrenceDerivedStatusType
```

Mesmo padrão de todo VO-enum já existente (`OccurrenceStatus`,
`ExpenseCategory`, `RecurringExpenseStatus`, `Frequency`) — imutável, sem
persistência própria (é sempre recomputado, nunca uma coluna do banco).

### `DomainRuleViolationException` (`Domain/DomainRuleViolationException.cs`)

```text
sealed class DomainRuleViolationException : Exception
  ctor(string message) : base(message)
```

Único tipo lançado por `Occurrence.MarkAsPaid`/`UndoPayment` para violação
de invariante de estado. Mensagem sempre PT-BR (Princípio VI), relayed
as-is pelo middleware global (ver `research.md` §5).

### `IRecurringExpenseRepository` (`Domain/Repositories/IRecurringExpenseRepository.cs`) — métodos novos

| Método | Retorno | Uso |
|---|---|---|
| `GetByReferencePeriodAsync(ReferencePeriod referencePeriod)` | `IReadOnlyCollection<RecurringExpense>` | `GetMonthlyPanelUseCase` (RF19–RF20) |
| `GetByOccurrenceIdAsync(Guid occurrenceId)` | `RecurringExpense?` | `MarkOccurrenceAsPaidUseCase`, `UndoOccurrencePaymentUseCase` |
| `UpdateAsync(RecurringExpense recurringExpense)` | `Task` | Persiste mutações feitas em um agregado já carregado no mesmo `DbContext` (chama `SaveChangesAsync()`, sem Unit of Work próprio) |

## Infrastructure

### `OccurrenceConfigurations` — mapeamento novo

| Coluna | Tipo SQL | Conversão |
|---|---|---|
| `PaidAmount` | `decimal(18,2)`, nullable | `Money?` ↔ `decimal?` |
| `PaymentDate` | `date`, nullable | `CalendarDate?` ↔ `DateOnly?` |

### Migration

Uma única migration nova, aditiva (`AddOccurrencePaymentTracking` ou nome
equivalente), adicionando as duas colunas acima à tabela `Occurrences` já
existente. Nenhuma migration existente é alterada (Princípio VII).

## Application (UseCases)

### `GetMonthlyPanelUseCase` (`Application/UseCases/GetMonthlyPanel/`)

**Input**: `GetMonthlyPanelUseCaseInput { string? Year, string? Month }`
(strings brutas — mesmo padrão de `CreateRecurringExpenseUseCaseInput.StartDate` —
para permitir que "ausente" e "não numérico" sejam distinguidos e
reportados em PT-BR pelo próprio UseCase, sem depender de mensagens de
model binding do ASP.NET Core; ver `research.md` §6).

**Lógica**:
1. Se `Year` e `Month` forem ambos `null`/vazios → competência atual
   (`ReferencePeriod.FromDate(_currentDateProvider.GetCurrentDate())`) —
   FR-002.
2. Se exatamente um dos dois estiver ausente → `FieldError("period", "Informe mês e ano juntos, ou nenhum dos dois.")`.
3. Senão, tenta `int.Parse` os dois; falha de parsing →
   `FieldError("period", "Mês e ano devem ser números válidos.")`.
4. Senão, tenta construir `new ReferencePeriod(year, month)`; captura
   `ArgumentException` (já PT-BR, ex. "O mês deve estar entre 1 e 12.") →
   `FieldError("period", ex.Message)` — EC16/CA14.
5. Em caso de qualquer erro acima → `Output.Failure(errors)`.
6. Senão: busca `repository.GetByReferencePeriodAsync(period)`; para cada
   agregado retornado, `GetOccurrencesForPeriod(period)`; para cada
   ocorrência, monta um item de saída com `GetDerivedStatus(today)`.

**Output**: `GetMonthlyPanelUseCaseOutput { bool IsSuccess; int? Year; int?
Month; IReadOnlyCollection<PanelOccurrenceData>? Occurrences;
IReadOnlyCollection<FieldError> Errors }`, onde:

```text
record PanelOccurrenceData(
    Guid Id,
    string Name,
    string Category,
    decimal ExpectedAmount,
    DateOnly DueDate,
    string DerivedStatus,       // "Paid" | "Overdue" | "DueSoon" | "Pending"
    decimal? PaidAmount,
    DateOnly? PaymentDate)
```

### `MarkOccurrenceAsPaidUseCase` (`Application/UseCases/MarkOccurrenceAsPaid/`)

**Input**: `MarkOccurrenceAsPaidUseCaseInput { Guid OccurrenceId, string?
PaidAmountRaw, string? PaymentDateRaw }`.

**Lógica** (FR-013, EC08–EC09):
1. `recurringExpense = await repository.GetByOccurrenceIdAsync(OccurrenceId)`;
   `null` → lança `KeyNotFoundException("Ocorrência não encontrada.")`
   (mapeado a `404` pelo middleware).
2. `occurrence = recurringExpense.FindOccurrence(OccurrenceId)!` (garantido
   não-nulo pela consulta acima).
3. Tenta interpretar `PaidAmountRaw` como número (vírgula como separador
   decimal, mesma lógica já usada no frontend/`CreateRecurringExpenseUseCase`
   para valores); vazio ou inválido → usa `occurrence.GetExpectedAmount()`.
4. Tenta interpretar `PaymentDateRaw` (`dd/MM/yyyy`); vazio ou inválido →
   usa `_currentDateProvider.GetCurrentDate()`.
5. Constrói `Money`/`CalendarDate` a partir dos valores resolvidos.
6. `recurringExpense.MarkOccurrenceAsPaid(OccurrenceId, paidAmount,
   paymentDate)` — pode lançar `DomainRuleViolationException` (mapeada a
   `400`).
7. `await repository.UpdateAsync(recurringExpense)`.
8. Retorna o item atualizado (mesmo formato de `PanelOccurrenceData`).

**Output**: `MarkOccurrenceAsPaidUseCaseOutput { PanelOccurrenceData Occurrence }`
(sem caminho de `Errors` de campo — as únicas falhas possíveis são "não
encontrada" e "já paga", ambas exceções mapeadas pelo middleware, não
`FieldError`s de forma).

### `UndoOccurrencePaymentUseCase` (`Application/UseCases/UndoOccurrencePayment/`)

**Input**: `UndoOccurrencePaymentUseCaseInput { Guid OccurrenceId }`.

**Lógica**: mesmo passo 1–2 acima; `recurringExpense.UndoOccurrencePayment(OccurrenceId)`
(pode lançar `DomainRuleViolationException` "ainda não foi paga", mapeada a
`400`); `UpdateAsync`; retorna o item atualizado.

**Output**: `UndoOccurrencePaymentUseCaseOutput { PanelOccurrenceData Occurrence }`.

## API (transporte HTTP)

Ver [`contracts/api-contract.md`](./contracts/api-contract.md) para a forma
completa de request/response de cada endpoint. Tipos novos, todos em
`backend/Api`:

| Tipo | Pasta | Papel |
|---|---|---|
| `MarkOccurrenceAsPaidDataRequest` | `/Requests` | Corpo de `PATCH .../payment` — `{ string? PaidAmount, string? PaymentDate }`, ambos opcionais (nenhuma anotação `[Required]` — FR-013) |
| `GetMonthlyPanelDataResponse` | `/Responses` | `{ ReferencePeriodDataResponse ReferencePeriod, IReadOnlyCollection<PanelOccurrenceDataResponse> Occurrences }` |
| `PanelOccurrenceDataResponse` | `/Responses` | `{ Guid Id, string Name, string Category, decimal ExpectedAmount, DateOnly DueDate, string Status, decimal? PaidAmount, DateOnly? PaymentDate }` — reaproveitado como item único em `MarkOccurrenceAsPaidDataResponse`/`UndoOccurrencePaymentDataResponse` |
| `MarkOccurrenceAsPaidDataResponse` | `/Responses` | `{ PanelOccurrenceDataResponse Occurrence }` |
| `UndoOccurrencePaymentDataResponse` | `/Responses` | `{ PanelOccurrenceDataResponse Occurrence }` |

`ReferencePeriodDataResponse` já existe (`004-api-despesa-recorrente`),
reaproveitado sem alteração.

## Fluxo de mapeamento (ponta a ponta) — `GET /api/v1/occurrences`

```text
HTTP GET ?year=&month= (query string, ambos string? opcionais)
  → OccurrencesController.Get(string? year, string? month)
  → GetMonthlyPanelUseCaseInput
  → IGetMonthlyPanelUseCase.ExecuteAsync
  → GetMonthlyPanelUseCaseOutput
      ├─ IsSuccess == true
      │    → GetMonthlyPanelDataResponseMapping
      │    → GetMonthlyPanelDataResponse
      │    → ApiResponse<GetMonthlyPanelDataResponse>.Success(...)  → 200
      └─ IsSuccess == false
           → Output.Errors (FieldError[]) → ApiError[]
           → ApiResponse<object>.Failure(...)  → 400

DomainRuleViolationException (não se aplica a este endpoint — apenas leitura)
KeyNotFoundException (não se aplica a este endpoint — período inválido é FieldError, não 404)
Exceção não tratada → ExceptionHandlingMiddleware → 500
```

## Fluxo de mapeamento — `PATCH/DELETE /api/v1/occurrences/{occurrenceId}/payment`

```text
HTTP PATCH body (JSON, MarkOccurrenceAsPaidDataRequest) | HTTP DELETE (sem corpo)
  → MarkOccurrenceAsPaidDataRequestMapping (apenas no PATCH)
  → MarkOccurrenceAsPaidUseCaseInput | UndoOccurrencePaymentUseCaseInput
  → UseCase.ExecuteAsync
      ├─ sucesso → *DataResponseMapping → ApiResponse<...>.Success(...)  → 200
      ├─ KeyNotFoundException ("Ocorrência não encontrada.")
      │    → ExceptionHandlingMiddleware → ApiResponse<object>.Failure([{field:null, message}])  → 404
      ├─ DomainRuleViolationException ("já está paga" / "ainda não foi paga")
      │    → ExceptionHandlingMiddleware → ApiResponse<object>.Failure([{field:null, message}])  → 400
      └─ Exceção não tratada
           → ExceptionHandlingMiddleware → ApiResponse<object>.Failure([erro genérico])  → 500
```

## Frontend (view model — não persistido, apenas na tela)

`painel-mensal-despesas.model.ts` define o formato de transporte
(`PanelOccurrenceResponse`, espelhando `PanelOccurrenceDataResponse`) e um
mapa de rótulo/cor por `Status` derivado, reproduzindo exatamente os
valores do script de `design/Main.dc.html`:

| `Status` (API) | Rótulo (PT-BR) | `bg` | `color` |
|---|---|---|---|
| `Paid` | "Paga" | `#E7F6EF` | `#0F7B4E` |
| `Overdue` | "Vencida" | `#FDECEA` | `#C0362C` |
| `DueSoon` | "Vence em breve" | `#FFF6E5` | `#9A6300` |
| `Pending` | "Pendente" | `#EEF2FF` | `#3446C9` |

Cores de categoria reaproveitadas de `despesa-recorrente.model.ts`
(`CATEGORY_COLORS`, `CATEGORY_OPTIONS`) — mesmo conjunto fechado de
categorias, sem duplicação.

Estado do componente (Signals, nenhum persistido fora da sessão da tela):
`occurrences` (lista bruta da API), `referencePeriod`, `editingOccurrenceId`,
`draftValor`, `draftData`, `loadStatus`. `computed` derivam: lista de itens
de exibição (rótulo/cor de status, flags `isEditing`/`paid`/`notPaid`),
`totalPrevisto`/`totalPago`/`totalPendente`, `vencidasCount`/
`venceEmBreveCount`/`totalAVencer` — mesma fórmula de `renderVals()` no
protótipo, agora sobre dados vindos da API em vez do array fixo de exemplo.

Métodos novos de navegação (FR-020/FR-021 — ver `research.md` §7):
`mesAnterior()`/`proximoMes()` calculam a competência de destino a partir
de `referencePeriod()` (um mês antes/depois, com virada de ano), atualizam
o signal `referencePeriod` e chamam `painel-mensal-despesas.service.ts`
novamente para recarregar `occurrences` a partir da nova competência
(mesmo endpoint `GET /api/v1/occurrences?year=&month=` já usado na carga
inicial — nenhum endpoint novo). O botão "Nova despesa" não tem estado de
componente próprio — é um `routerLink="/despesas/nova"` no template.

## Frontend — UX transversais acrescentadas em 2026-09-08 (FR-022 a FR-030)

Ver `research.md` §9–§14 para a justificativa de cada decisão abaixo.
Nenhum tipo novo é introduzido no Domain/Application/API — todas as
mudanças abaixo são de frontend (utilitários compartilhados + estado de
componente).

### Novos utilitários compartilhados (`frontend/src/app/shared/`)

| Arquivo | Export | Assinatura | Usado por |
|---|---|---|---|
| `currency-mask.util.ts` | `maskCurrencyDigits` | `(raw: string) => string` | `painel-mensal-despesas.component.ts` (`onDraftValorInput`), `cadastro-despesa-recorrente.component.ts` (`onValorInput`) |
| `currency-format.util.ts` | `formatEUR` | `(value: number) => string` | Substitui `formatBRL` em `painel-mensal-despesas.component.ts` e o `Intl.NumberFormat(...BRL...)` de `valorFmt` em `cadastro-despesa-recorrente.component.ts` |

Ambos são funções puras, sem estado e sem dependência de Angular — testáveis
isoladamente (`currency-mask.util.spec.ts`, `currency-format.util.spec.ts`).

### `PainelMensalDespesasComponent` — alterações

| Elemento | Mudança |
|---|---|
| `draftData` (signal) | Passa a ser alimentado por um `<input type="date">` (valor ISO `yyyy-MM-dd`), em vez de `type="text"` livre |
| `onDraftDataInput` | Sem mudança de assinatura — apenas o valor recebido do evento passa a já vir validado/normalizado pelo navegador (ISO ou vazio) |
| `confirmarPagamento` | Antes de chamar `painelService.markOccurrenceAsPaid(...)`, converte `draftData()` de ISO para `dd/MM/yyyy` via nova função pura `isoToBrDate` (local ao arquivo do componente, ao lado de `formatIsoDateAsBR`/`formatDateAsBR` já existentes) — contrato de API inalterado (`paymentDate` continua `dd/MM/yyyy`, ver `research.md` §12) |
| `onDraftValorInput` | Aplica `maskCurrencyDigits` ao valor bruto do evento antes de gravar em `draftValor` |
| `formatBRL` (função local) | Removida; usos substituídos por `formatEUR` (utilitário compartilhado) |
| Template — grade de 3 cartões | `grid-cols-3 min-[481px]:max-[720px]:grid-cols-2 max-[480px]:grid-cols-1` (FR-026/FR-027 — ver `research.md` §13) |
| Template — colunas da linha de ocorrência | `max-[720px]:order-{1..5}` em `nome`/`status`/`valor`/`dia`/`ações`, nesta ordem (FR-026) |
| Template — banners de alerta | `max-[480px]:basis-full` (ou equivalente) para ocupar largura total em telas ≤480px (FR-027) |
| Template — elementos clicáveis | `cursor-pointer` em todo botão/link/seta/ícone de ação/input de data; `disabled:cursor-default` nos botões que podem ficar desabilitados (FR-022 — ver `research.md` §9) |

### `CadastroDespesaRecorrenteComponent` — alterações

| Elemento | Mudança |
|---|---|
| `dataInicio` (signal) | Passa a guardar diretamente o valor ISO de um `<input type="date">`; `parseDataInicio`/`toIsoDate` são removidas (o payload `startDate` já era ISO — nenhuma mudança de contrato) |
| `dataInicioError` | Simplificado para checar apenas presença (o navegador já impede submissão de data inválida/incompleta em `type="date"`) |
| `onValorInput` | Aplica `maskCurrencyDigits` ao valor bruto do evento antes de gravar em `valor` |
| `valorFmt` | Passa a usar `formatEUR` (utilitário compartilhado) em vez de `Intl.NumberFormat(...BRL...)` |
| `hasUnsavedData` (computed novo) | `true` se `nome`/`valor`/`dia`/`dataInicio`/`observacao` não vazios, ou `categoria`/`status` diferentes dos padrões (`'Housing'`/`'ativa'`) — comparação de valor, não flag de "tocou" (FR-024, edge case 2026-09-08) |
| `showExitConfirmDialog` (signal novo) | Controla a exibição do modal de confirmação de saída |
| `onClickVoltar()` (método novo) | `hasUnsavedData()` falso → navega direto para `/`; verdadeiro → `showExitConfirmDialog.set(true)` (FR-023/FR-024) |
| `onCancelExit()` (método novo) | Fecha o modal sem navegar |
| `onConfirmExit()` (método novo) | Fecha o modal e navega para `/`, descartando os dados (sem salvamento parcial) |
| Template — cabeçalho do formulário | Novo botão/link "Voltar ao painel" chamando `onClickVoltar()` (posição conforme `design/Cadastro.dc.html`) |
| Template — estado de sucesso | Novo botão "Voltar ao painel" (`routerLink="/"`, sem verificação de dados não salvos), ao lado do já existente "Cadastrar outra despesa" (`onNovaDespesa()`) — FR-025 |
| Template — modal de confirmação | Novo bloco condicional (`@if (showExitConfirmDialog())`), reproduzindo `design/Cadastro.dc.html` (título, texto de aviso, botões "Continuar editando"/"Sair sem salvar") |
| Template — elementos clicáveis | `cursor-pointer`/`disabled:cursor-default` conforme FR-022 |
| `imports` do `@Component` | Ganha `RouterLink` (`@angular/router`, já dependência do projeto — `research.md` §7) |
