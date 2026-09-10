# Phase 1 Data Model: Editar Despesa Recorrente (Backend)

**Feature**: `007-edit-recurring-expense` | **Date**: 2026-09-10

## Domain — `RecurringExpense` (aggregate já existente, estendido)

Nenhum campo novo. Oito novos métodos de negócio, cada um substituindo a
referência interna do campo correspondente por um Value Object novo, já
validado pelo próprio construtor do VO (ver `research.md` §1):

| Método | Assinatura | Efeito |
|---|---|---|
| Renomear | `Rename(ExpenseName newName)` | `_name = newName;` |
| Trocar categoria | `ChangeCategory(ExpenseCategory newCategory)` | `_category = newCategory;` |
| Trocar valor previsto mensal | `ChangeMonthlyAmount(Money newMonthlyAmount)` | `_monthlyAmount = newMonthlyAmount;` |
| Trocar dia de vencimento | `ChangeDueDay(DueDay newDueDay)` | `_dueDay = newDueDay;` |
| Trocar data de início | `ChangeStartDate(CalendarDate newStartDate)` | `_startDate = newStartDate;` |
| Trocar observação | `ChangeNote(Note newNote)` | `_note = newNote;` |
| Pausar | `Pause()` | `_status = new RecurringExpenseStatus(RecurringExpenseStatusType.Paused);` |
| Reativar | `Reactivate(ReferencePeriod currentReferencePeriod)` | `_status = new RecurringExpenseStatus(RecurringExpenseStatusType.Active);` seguido de `GenerateOccurrenceForCurrentPeriodIfDue(currentReferencePeriod)` |

Nenhum desses métodos toca `_occurrences` (exceto `Reactivate`, através do
método privado compartilhado abaixo) — consistente com FR-003/FR-004/FR-008
da spec.

### Método privado compartilhado (novo)

`GenerateOccurrenceForCurrentPeriodIfDue(ReferencePeriod currentReferencePeriod)` —
extraído da lógica hoje inline no construtor (ver `research.md` §2):

```text
startPeriod = ReferencePeriod.FromDate(_startDate.GetValue())
se currentReferencePeriod >= startPeriod
   E GetOccurrencesForPeriod(currentReferencePeriod).Count == 0:
    calcular dueDate do mês (Math.Min(_dueDay, DaysInMonth))
    _occurrences.Add(new Occurrence(currentReferencePeriod, dueDate, _name, _category, _monthlyAmount))
```

Chamado pelo construtor (quando `status == Active`) e por `Reactivate`
(incondicionalmente, após trocar o status) — nenhuma duplicação da regra
entre os dois pontos de chamada.

**Validação de negócio**: nenhuma validação nova é adicionada. Cada método
recebe um Value Object já construído (e já validado pelo seu próprio
construtor); nenhuma checagem adicional de estado/transição é feita por
`Pause`/`Reactivate` (confirmado via `/speckit-clarify`: nenhuma restrição
adicional na edição da data de início; reativação sempre tenta gerar a
ocorrência, protegida apenas pela condição de data e não duplicidade já
existente em `GenerateOccurrenceForCurrentPeriodIfDue`).

## Application — dois novos UseCases

### `RecurringExpenseData` (novo `record` — definido em `GetRecurringExpenseById`, reaproveitado por `UpdateRecurringExpense`)

DTO de dados compartilhado entre os dois UseCases desta feature, no mesmo
espírito de `PanelOccurrenceData` (definido em `GetMonthlyPanel` e
reaproveitado por `MarkOccurrenceAsPaidUseCaseOutput`/
`UndoOccurrencePaymentUseCaseOutput`) — não viola a regra de Input/Output
não compartilhados (Princípio XI), que se aplica aos tipos `Input`/`Output`
de cada UseCase, não a um DTO de dados aninhado reaproveitado entre eles.

| Campo | Tipo C# |
|---|---|
| `Id` | `Guid` |
| `Name` | `string` |
| `Category` | `string` |
| `MonthlyAmount` | `decimal` |
| `DueDay` | `int` |
| `StartDate` | `DateOnly` |
| `Frequency` | `string` |
| `Status` | `string` |
| `Note` | `string?` |

### `GetRecurringExpenseByIdUseCaseInput`

| Campo | Tipo C# |
|---|---|
| `Id` | `Guid` (`required`) |

### `GetRecurringExpenseByIdUseCaseOutput`

Sem `IsSuccess`/`Errors` — não há caminho de falha de validação nesta
consulta; "não encontrado" é sinalizado por exceção antes de qualquer
`Output` existir (ver `research.md` §4).

| Campo | Tipo C# |
|---|---|
| `RecurringExpense` | `RecurringExpenseData` |

### `UpdateRecurringExpenseUseCaseInput`

Espelha `CreateRecurringExpenseUseCaseInput`, sem `Frequency` (não
editável — FR-001):

| Campo | Tipo C# |
|---|---|
| `Id` | `Guid` (`required`) |
| `Name` | `string` (`required`) |
| `Category` | `string` (`required`) |
| `MonthlyAmount` | `decimal` (`required`) |
| `DueDay` | `int` (`required`) |
| `StartDate` | `string` (`required`) |
| `Status` | `string` (`required`) |
| `Note` | `string?` |

### `UpdateRecurringExpenseUseCaseOutput`

Mesmo padrão `Success`/`Failure` de `CreateRecurringExpenseUseCaseOutput`,
reaproveitando `FieldError` (definido em `CreateRecurringExpense`, já
reaproveitado por `GetMonthlyPanel`) e `RecurringExpenseData` (acima).

| Campo | Tipo C# | Presença |
|---|---|---|
| `IsSuccess` | `bool` | sempre |
| `RecurringExpense` | `RecurringExpenseData?` | apenas quando `IsSuccess == true` |
| `Errors` | `IReadOnlyCollection<FieldError>` | sempre (vazia quando `IsSuccess == true`) |

### Orquestração de `UpdateRecurringExpenseUseCase.ExecuteAsync`

```text
1. recurringExpense = GetByIdAsync(input.Id)
   se null: throw KeyNotFoundException("Despesa recorrente não encontrada.")
2. Para cada campo (name, category, monthlyAmount, dueDay, startDate, status),
   construir o Value Object correspondente em try/catch, acumulando FieldError
   (mesmo padrão de CreateRecurringExpenseUseCase); note não precisa de
   validação (Note aceita qualquer string?, incluindo null)
3. se errors.Count > 0: return Failure(errors)   — nenhum método do aggregate é chamado
4. Comparar cada VO novo com o valor atual do aggregate (GetValue()); invocar
   apenas os métodos cujo valor mudou:
     - name != atual       → Rename(name)
     - category != atual   → ChangeCategory(category)
     - monthlyAmount != atual → ChangeMonthlyAmount(monthlyAmount)
     - dueDay != atual     → ChangeDueDay(dueDay)
     - startDate != atual  → ChangeStartDate(startDate)
     - note != atual       → ChangeNote(note)
     - status: Active→Paused  → Pause()
               Paused→Active  → Reactivate(currentReferencePeriod via ICurrentDateProvider)
               sem mudança    → nenhuma chamada
5. UpdateAsync(recurringExpense)  (SaveChangesAsync via Unit of Work)
6. return Success(RecurringExpenseData a partir do aggregate já atualizado)
```

## API — dois novos endpoints no `RecurringExpensesController`

### `UpdateRecurringExpenseDataRequest` (`/Requests`)

Corpo de `PUT /api/v1/recurring-expenses/{id}`. Mesmos campos de
`CreateRecurringExpenseDataRequest`, sem `Frequency`:

| Campo | Tipo C# | Anotação | `ErrorMessage` (PT-BR) |
|---|---|---|---|
| `Name` | `string?` | `[Required]` | "Nome é obrigatório." |
| `Category` | `string?` | `[Required]` | "Categoria é obrigatória." |
| `MonthlyAmount` | `decimal?` | `[Required]` | "Valor previsto mensal é obrigatório." |
| `DueDay` | `int?` | `[Required]` | "Dia de vencimento é obrigatório." |
| `StartDate` | `string?` | `[Required]` | "Data de início é obrigatória." |
| `Status` | `string?` | `[Required]` | "Status é obrigatório." |
| `Note` | `string?` | — (opcional) | — |

`MonthlyAmount`/`DueDay` anuláveis pelo mesmo motivo já registrado em
`004-api-despesa-recorrente/data-model.md` (distinguir "ausente" de "zero").

**Relationships**: mapeado 1:1 para `UpdateRecurringExpenseUseCaseInput`
(mais o `id` da rota) via `UpdateRecurringExpenseDataRequestMapping`.

### `RecurringExpenseDataResponse` (`/Responses`)

Corpo de sucesso compartilhado por `GET` (`200`) e `PUT` (`200`) — os dois
endpoints devolvem exatamente os mesmos campos (FR-011/FR-017):

| Campo | Tipo C# |
|---|---|
| `Id` | `Guid` |
| `Name` | `string` |
| `Category` | `string` |
| `MonthlyAmount` | `decimal` |
| `DueDay` | `int` |
| `StartDate` | `DateOnly` |
| `Frequency` | `string` |
| `Status` | `string` |
| `Note` | `string?` |

**Relationships**: mapeado a partir de `RecurringExpenseData`
(Application, reaproveitado por ambos os UseCases) via
`RecurringExpenseDataResponseMapping` — uma única extensão
`ToDataResponse(this RecurringExpenseData data)` usada pelas duas ações do
controller.

### `ApiResponse<TData>`/`ApiError`

Reaproveitados sem alteração (já existentes desde a feature 004).

## Fluxo de mapeamento (ponta a ponta)

```text
GET /api/v1/recurring-expenses/{id}
  → GetRecurringExpenseByIdUseCaseInput { Id = id }
  → IGetRecurringExpenseByIdUseCase.ExecuteAsync
      ├─ encontrado  → GetRecurringExpenseByIdUseCaseOutput.RecurringExpense (RecurringExpenseData)
      │                 → RecurringExpenseDataResponseMapping.ToDataResponse(...)
      │                 → ApiResponse<RecurringExpenseDataResponse>.Success(...)  → 200
      └─ não encontrado → throw KeyNotFoundException → ExceptionHandlingMiddleware → 404

PUT /api/v1/recurring-expenses/{id}
  → UpdateRecurringExpenseDataRequest (model binding + [Required] PT-BR)
  → UpdateRecurringExpenseDataRequestMapping.ToUseCaseInput(id)
  → UpdateRecurringExpenseUseCaseInput
  → IUpdateRecurringExpenseUseCase.ExecuteAsync
      ├─ não encontrado        → throw KeyNotFoundException → ExceptionHandlingMiddleware → 404
      ├─ IsSuccess == false    → Errors (FieldError[]) → ApiError[] (ToApiErrors, já existente)
      │                          → ApiResponse<RecurringExpenseDataResponse>.Failure(...)  → 400
      └─ IsSuccess == true     → RecurringExpenseData → RecurringExpenseDataResponseMapping.ToDataResponse(...)
                                 → ApiResponse<RecurringExpenseDataResponse>.Success(...)  → 200

ModelState inválido (forma/presença, PUT)
  → ApiBehaviorOptions.InvalidModelStateResponseFactory (já configurado desde a feature 004)
  → ApiResponse<RecurringExpenseDataResponse>.Failure(...)  → 400  (mesmo formato acima)

Exceção não tratada (ex.: banco indisponível)
  → ExceptionHandlingMiddleware (já existente)
  → ApiResponse<RecurringExpenseDataResponse>.Failure([erro genérico])  → 500
```
