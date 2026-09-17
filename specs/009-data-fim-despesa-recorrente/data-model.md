# Phase 1 Data Model: Data de Fim da Despesa Recorrente

**Feature**: `009-data-fim-despesa-recorrente` | **Date**: 2026-09-17

## Domain — `RecurringExpense` (aggregate já existente, estendido)

### Novo campo

| Campo | Tipo | Observação |
|---|---|---|
| `_endDate` | `CalendarDate` | Mesmo Value Object já usado por `_startDate`; recebido no construtor público logo após `startDate` (refinamento, seção Domain). |

### Construtor público — assinatura alterada

```text
RecurringExpense(
    ExpenseName name,
    ExpenseCategory category,
    Money monthlyAmount,
    DueDay dueDay,
    CalendarDate startDate,
    CalendarDate endDate,              // NOVO — logo após startDate
    Frequency frequency,
    RecurringExpenseStatus status,
    Note note,
    ReferencePeriod currentReferencePeriod)
```

Corpo do construtor passa a:
1. `ValidateVigencia(startDate, endDate)` (ver `research.md` §2) — lança
   `DomainRuleViolationException` se violar "fim > início" ou "vigência ≤ 1
   ano".
2. `ValidateEndDateNotInPast(endDate, currentReferencePeriod)` (ver
   `research.md` §3) — lança `DomainRuleViolationException` se a
   competência de `endDate` for anterior a `currentReferencePeriod`.
3. Atribuir `_endDate = endDate`.
4. Chamar `GenerateOccurrencesForVigencia(currentReferencePeriod)`
   **incondicionalmente** (sem checar `status == Active` — decisão 1 do
   refinamento; RF11 do domínio original deixa de valer para o cadastro).

Construtor privado de reidratação do EF Core ganha `CalendarDate endDate`
como novo parâmetro (sem nenhuma validação — dado já persistido é
confiável), atribuído diretamente a `_endDate`.

### Novo getter

| Método | Retorno |
|---|---|
| `GetEndDate()` | `CalendarDate` |

### Novos métodos privados

| Método | Assinatura | Efeito |
|---|---|---|
| Validar vigência | `static void ValidateVigencia(CalendarDate startDate, CalendarDate endDate)` | Lança `DomainRuleViolationException("A data de fim deve ser posterior à data de início.")` se `endDate <= startDate`; lança `DomainRuleViolationException("A vigência não pode ultrapassar 1 ano a partir da data de início.")` se `endDate > startDate.AddYears(1)`. |
| Validar fim não retroativo | `static void ValidateEndDateNotInPast(CalendarDate endDate, ReferencePeriod currentReferencePeriod)` | Lança `DomainRuleViolationException("A data de fim não pode estar no passado.")` se `ReferencePeriod.FromDate(endDate.GetValue()) < currentReferencePeriod`. |
| Gerar 1 competência (extraído) | `void GenerateOccurrenceForPeriodIfMissing(ReferencePeriod period)` | Mesmo corpo que hoje existe em `GenerateOccurrenceForCurrentPeriodIfDue`, sem a checagem de `currentReferencePeriod >= startPeriod` (que passa a viver só no chamador do laço, ver abaixo). |
| Gerar vigência inteira (novo) | `void GenerateOccurrencesForVigencia(ReferencePeriod currentReferencePeriod)` | Se `currentReferencePeriod < ReferencePeriod.FromDate(_startDate.GetValue())`: não gera nada (EC06). Senão, laço de `currentReferencePeriod` até `ReferencePeriod.FromDate(_endDate.GetValue())` (inclusive), chamando `GenerateOccurrenceForPeriodIfMissing` a cada iteração, avançando com `ReferencePeriod.Next()` (novo, ver `research.md` §1). |

`GenerateOccurrenceForCurrentPeriodIfDue` é removido; `Reactivate` passa a
chamar diretamente `GenerateOccurrenceForPeriodIfMissing(currentReferencePeriod)`
— mas só depois de confirmar `currentReferencePeriod >=
ReferencePeriod.FromDate(_startDate.GetValue())`, condição que antes vivia
dentro do método removido e precisa ser preservada em `Reactivate` para não
mudar seu comportamento externo (fora de escopo revisar `Reactivate` nesta
feature — ver `research.md` §4 e `spec.md`, Assumptions).

### Métodos existentes com comportamento alterado

| Método | Mudança |
|---|---|
| `ChangeStartDate(CalendarDate newStartDate)` | Passa a chamar `ValidateVigencia(newStartDate, _endDate)` **antes** de atribuir; agora pode lançar `DomainRuleViolationException` (FR-012). **Não** chama `ValidateEndDateNotInPast` (research.md §3). Nenhum efeito sobre `_occurrences`. |

### Novo método de negócio

| Método | Assinatura | Efeito |
|---|---|---|
| Trocar data de fim | `ChangeEndDate(CalendarDate newEndDate, ReferencePeriod currentReferencePeriod)` | Ver algoritmo completo em `research.md` §5: valida (`ValidateVigencia` + `ValidateEndDateNotInPast`); se reduzindo a competência-teto, verifica ocorrências pagas no intervalo removido **antes** de mutar qualquer estado (lança `DomainRuleViolationException` se houver, sem alterar `_endDate` nem `_occurrences`); caso contrário atualiza `_endDate` e gera (extensão) ou remove (redução) as ocorrências das competências que mudaram de status de cobertura; competência-teto igual é no-op sobre `_occurrences`. |

### Invariantes (resumo)

| Regra | Onde é validada | Mensagem PT-BR |
|---|---|---|
| Fim > início | `ValidateVigencia`, chamada por: construtor, `ChangeStartDate`, `ChangeEndDate` | "A data de fim deve ser posterior à data de início." |
| Vigência ≤ 1 ano | `ValidateVigencia` (mesmos 3 chamadores) | "A vigência não pode ultrapassar 1 ano a partir da data de início." |
| Fim não pode estar no passado | `ValidateEndDateNotInPast`, chamada por: construtor, `ChangeEndDate` (**não** `ChangeStartDate`) | "A data de fim não pode estar no passado." |
| Redução não pode excluir ocorrência paga | `ChangeEndDate`, antes de mutar estado | Mensagem própria, ex.: "Não é possível reduzir a vigência: existe uma ocorrência já paga no período que seria removido." |

## Domain — `ReferencePeriod` (Value Object já existente, estendido)

| Método novo | Assinatura | Efeito |
|---|---|---|
| Avançar competência | `Next()` | `Month == 12 ? new ReferencePeriod(Year + 1, 1) : new ReferencePeriod(Year, Month + 1)` |

## Application — `CreateRecurringExpense` (UseCase já existente, estendido)

### `CreateRecurringExpenseUseCaseInput`

| Campo novo | Tipo C# |
|---|---|
| `EndDate` | `string` (`required`, formato `yyyy-MM-dd`, mesmo padrão de `StartDate`) |

### `CreateRecurringExpenseUseCase.ExecuteAsync` — orquestração alterada

```text
1. Construir cada Value Object em try/catch, acumulando FieldError
   (name, category, monthlyAmount, dueDay, startDate, frequency, status —
   já existente) + NOVO: endDate (mesma lógica de parse de startDate;
   texto malformado → FieldError("endDate", "Data de fim inválida."))
2. se errors.Count > 0: return Failure(errors)   — como já era
3. NOVO: try { recurringExpense = new RecurringExpense(..., startDate!,
   endDate!, ..., currentReferencePeriod); }
   catch (DomainRuleViolationException ex) {
       return Failure([new FieldError("endDate", ex.Message)]);
   }
   (cobre as duas invariantes cruzadas + "não pode estar no passado")
4. AddAsync(recurringExpense)   — como já era
5. return Success(..., endDate: recurringExpense.GetEndDate().GetValue(), occurrences)
   — occurrences agora pode ter de 1 a ~13 itens, mesmo com status Paused
```

### `CreateRecurringExpenseUseCaseOutput`

| Campo novo (apenas em `Success`) | Tipo C# |
|---|---|
| `EndDate` | `DateOnly` |

`OccurrenceData`/`FieldError`: sem mudança de forma.

## Application — `GetRecurringExpenseById` (UseCase já existente, estendido)

### `RecurringExpenseData` (record compartilhado, definido aqui)

| Campo novo | Tipo C# |
|---|---|
| `EndDate` | `DateOnly` |

## Application — `UpdateRecurringExpense` (UseCase já existente, estendido)

### `UpdateRecurringExpenseUseCaseInput`

| Campo novo | Tipo C# |
|---|---|
| `EndDate` | `string` (`required`, mesmo formato de `StartDate`) |

### `UpdateRecurringExpenseUseCase.ExecuteAsync` — orquestração alterada

```text
1. GetByIdAsync(input.Id) ou throw KeyNotFoundException  — como já era
2. Construir Value Objects (name, category, monthlyAmount, dueDay,
   startDate, status — já existente) + NOVO: endDate (mesmo parse de
   startDate)
3. se errors.Count > 0: return Failure(errors)  — como já era (só shape/
   parse; nenhuma regra cruzada checada ainda aqui)
4. Aplicar incondicionalmente os campos sem risco de exceção (já existente):
   Rename/ChangeCategory/ChangeMonthlyAmount/ChangeDueDay/ChangeNote
5. NOVO: se startDate mudou:
     try { recurringExpense.ChangeStartDate(startDate); }
     catch (DomainRuleViolationException ex) {
         crossFieldErrors.Add(new FieldError("startDate", ex.Message));
     }
6. NOVO: se endDate mudou:
     try { recurringExpense.ChangeEndDate(endDate, currentReferencePeriod); }
     catch (DomainRuleViolationException ex) {
         crossFieldErrors.Add(new FieldError("endDate", ex.Message));
     }
7. NOVO: se crossFieldErrors.Count > 0: return Failure(crossFieldErrors)
   — UpdateAsync NUNCA é chamado; como o aggregate mutado só existe em
   memória e SaveChangesAsync não roda, nenhum campo é persistido (FR-009/
   FR-012 — "nenhum campo é alterado")
8. Aplicar mudança de status (Pause/Reactivate) — como já era
9. UpdateAsync(recurringExpense)  — como já era
10. return Success(RecurringExpenseData com EndDate atualizado)
```

**Ordem de aplicação (passo 4 antes de 5/6)**: os campos "seguros"
(nome, categoria, valor, dia, observação) são aplicados primeiro porque
nunca lançam; `startDate`/`endDate` são isolados por último porque são os
únicos que podem exigir descartar a operação inteira sem persistir nada —
mesmo efeito prático de "nenhum campo é alterado" que uma trasação
explícita daria, sem precisar de uma (Princípio VII: EF Core
`SaveChangesAsync` já é a Unit of Work; nada é escrito até o passo 9).

### `UpdateRecurringExpenseUseCaseOutput`

Sem mudança de forma (`RecurringExpenseData` já ganhou `EndDate` acima).

## Api — `RecurringExpensesController` (endpoints já existentes, request/response estendidos)

### `CreateRecurringExpenseDataRequest` (`/Requests`)

| Campo novo | Tipo C# | Anotação | `ErrorMessage` (PT-BR) |
|---|---|---|---|
| `EndDate` | `string?` | `[Required]` | "Data de fim é obrigatória." |

### `UpdateRecurringExpenseDataRequest` (`/Requests`)

| Campo novo | Tipo C# | Anotação | `ErrorMessage` (PT-BR) |
|---|---|---|---|
| `EndDate` | `string?` | `[Required]` | "Data de fim é obrigatória." |

### `CreateRecurringExpenseDataResponse` (`/Responses`)

| Campo novo | Tipo C# |
|---|---|
| `EndDate` | `DateOnly` |

`Occurrences` (`OccurrenceDataResponse[]`, forma já existente): sem mudança
de forma, mas passa a ter de 1 a ~13 itens.

### `RecurringExpenseDataResponse` (`/Responses`, compartilhado por `GET`/`PUT`)

| Campo novo | Tipo C# |
|---|---|
| `EndDate` | `DateOnly` |

### Mappings (`/Mappings`)

- `CreateRecurringExpenseDataRequestMapping.ToUseCaseInput()`: mapeia
  `request.EndDate!` → `Input.EndDate`.
- `UpdateRecurringExpenseDataRequestMapping.ToUseCaseInput(id)`: mapeia
  `request.EndDate!` → `Input.EndDate`.
- `CreateRecurringExpenseDataResponseMapping`/
  `RecurringExpenseDataResponseMapping`: mapeiam `EndDate` da
  Output/`RecurringExpenseData` para a `DataResponse` — sem lógica
  adicional, mesmo padrão de `StartDate`.

### `ApiResponse<TData>`/`ApiError`

Reaproveitados sem alteração.

## Infrastructure — `RecurringExpenseConfigurations` + nova migração

### `RecurringExpenseConfigurations` (`/Configs`)

Nova propriedade mapeada, mesmo padrão de `_startDate`:

```csharp
builder.Property<CalendarDate>("_endDate")
    .HasColumnName("EndDate")
    .HasConversion(vo => vo.GetValue(), value => new CalendarDate(value))
    .HasColumnType("date")
    .IsRequired();
```

### Nova migração EF Core (`/Migrations`)

Três passos (ver `research.md` §8 para o SQL completo):
1. `AddColumn<DateOnly>("EndDate", "RecurringExpenses", type: "date",
   nullable: true)`.
2. `Sql("UPDATE \"RecurringExpenses\" SET \"EndDate\" = \"StartDate\" +
   INTERVAL '1 year' WHERE \"EndDate\" IS NULL;")` — backfill por linha
   (User Story 4 / FR-011).
3. `AlterColumn<DateOnly>("EndDate", "RecurringExpenses", type: "date",
   nullable: false, oldNullable: true)`.

Nenhuma outra tabela (`Occurrences`) muda de schema — a geração/exclusão em
lote usa as colunas já existentes.

### `RecurringExpenseRepository` (`/Repositories`)

Assinatura inalterada (`AddAsync`/`GetByIdAsync`/`UpdateAsync`/etc.); ver
`research.md` §6 para o risco de comportamento (não de assinatura) em torno
da remoção de `Occurrence`s rastreadas — a confirmar por
`Infrastructure.Tests` durante a implementação, com o mesmo tipo de ajuste
cirúrgico (marcar `EntityState` explicitamente) já usado por
`007-edit-recurring-expense` (T041) caso o comportamento padrão do EF Core
não se confirme.

## Frontend — `despesa-recorrente.model.ts`

| Tipo | Campo novo | Tipo TS |
|---|---|---|
| `CreateRecurringExpenseRequest` | `endDate` | `string` |
| `CreateRecurringExpenseResponse` | `endDate` | `string` |
| `RecurringExpenseDetailResponse` | `endDate` | `string` |
| `UpdateRecurringExpenseRequest` | `endDate` | `string` (herdado automaticamente via `Omit<CreateRecurringExpenseRequest, 'frequency'>`) |

`OccurrenceResponse`/`ApiEnvelope`/`FieldError`: sem mudança de forma.

## Frontend — `shared/recurring-expense-form.util.ts`

Nova função, mesmo estilo de `getDataInicioError`:

```typescript
export function getDataFimError(dataInicio: string, dataFim: string): string | null {
  if (!dataFim.trim()) return 'Data de fim é obrigatória.';
  if (dataInicio && dataFim <= dataInicio) return 'A data de fim deve ser posterior à data de início.';
  if (dataInicio && dataFim > addYearsIso(dataInicio, 1)) return 'A vigência não pode ultrapassar 1 ano a partir da data de início.';
  return null;
}
```

mais uma função utilitária `addYearsIso(iso: string, years: number): string`
(mesma lógica já prototipada em `design/Cadastro.dc.html`).

## Frontend — `CadastroDespesaRecorrenteComponent` / `EditarDespesaRecorrenteComponent`

Mesmo padrão já usado para `dataInicio` nos dois componentes:

| Elemento | Adição |
|---|---|
| Signal | `readonly dataFim = signal('')` |
| `touched` | ganha chave `dataFim: boolean` |
| Handlers | `onDataFimInput(event)` / `onDataFimBlur()` |
| Validação | `readonly dataFimError = computed(() => getDataFimError(this.dataInicio(), this.dataFim()))` |
| Exibição de erro | `readonly showDataFimError = computed(...)` — mesmo padrão de `showDataInicioError` |
| `isFormValid` | passa a incluir `!this.dataFimError()` |
| `hasUnsavedData`/`initialSnapshot` (edição) | passam a considerar `dataFim` |
| Payload (`onSalvar`) | `endDate: this.dataFim()` adicionado ao `CreateRecurringExpenseRequest`/`UpdateRecurringExpenseRequest` |
| Campos API conhecidos (`ApiField`/`KNOWN_API_FIELDS`) | ganham `'endDate'` nos dois componentes |
| Pré-carregamento (edição, `toFields`) | `dataFim: detail.endDate` |

Nenhuma mudança em `DespesaPreviewComponent` ou `DespesaRecorrenteService`
além da forma dos tipos já listados acima (o serviço só repassa o payload,
sem lógica própria).
