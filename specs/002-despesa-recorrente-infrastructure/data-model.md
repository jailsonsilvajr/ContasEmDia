# Phase 1 Data Model: Infrastructure de Despesa Recorrente (Persistência)

Este documento descreve o mapeamento relacional do aggregate `RecurringExpense` e da entidade `Occurrence` (já existentes e fechados no Domain — ver `backend/Domain/Aggregates/RecurringExpense.cs` e `backend/Domain/Entities/Occurrence.cs`). Nenhuma regra de negócio é definida aqui; apenas a representação de persistência (FR-010).

## Tabela `RecurringExpenses`

Mapeia o aggregate `RecurringExpense` (raiz de agregação, único `DbSet<T>` exposto pelo `ContasEmDiaDbContext`).

| Coluna | Tipo SQL Server | Origem (Domain) | Nulável | Observações |
|---|---|---|---|---|
| `Id` | `uniqueidentifier` | `RecurringExpense.GetId()` | Não | Chave primária |
| `Name` | `nvarchar(200)` | `ExpenseName.GetValue()` | Não | Via `HasConversion` |
| `Category` | `int` | `ExpenseCategory.GetValue()` (enum `ExpenseCategoryType`) | Não | Via `HasConversion`; armazenado como int do enum |
| `MonthlyAmount` | `decimal(18,2)` | `Money.GetValue()` | Não | Via `HasConversion` |
| `DueDay` | `int` | `DueDay.GetValue()` | Não | Via `HasConversion`; 1–31 |
| `StartDate` | `date` | `CalendarDate.GetValue()` (`DateOnly`) | Não | Via `HasConversion` |
| `Frequency` | `int` | `Frequency.GetValue()` (enum `FrequencyType`) | Não | Via `HasConversion` |
| `Status` | `int` | `RecurringExpenseStatus.GetValue()` (enum `RecurringExpenseStatusType`) | Não | Via `HasConversion` |
| `Note` | `nvarchar(1000)` | `Note.GetValue()` | Sim | Via `HasConversion`; `Note` pode envolver `string?` |

**Coleção de posse**: a lista privada `_occurrences` de `RecurringExpense` é mapeada via `UsePropertyAccessMode(PropertyAccessMode.Field)` apontando para o campo de apoio, sem expor uma propriedade pública no Domain (RF01 do refinamento). A navegação é 1:N para `Occurrences`, com carregamento explícito (`Include`) nas consultas do repositório (`GetByIdAsync`, `GetActiveAsync`).

## Tabela `Occurrences`

Mapeia a entidade dependente `Occurrence`, sempre associada a exatamente uma `RecurringExpense` (FR-008), via FK explícita — não owned entity.

| Coluna | Tipo SQL Server | Origem (Domain) | Nulável | Observações |
|---|---|---|---|---|
| `Id` | `uniqueidentifier` | `Occurrence.GetId()` | Não | Chave primária |
| `RecurringExpenseId` | `uniqueidentifier` | (FK, sem getter no Domain — mapeada como shadow FK ou via navegação de posse) | Não | FK obrigatória para `RecurringExpenses.Id`; `OnDelete` não especificado nesta feature (sem operação de exclusão em escopo) |
| `ReferenceYear` | `int` | `ReferencePeriod.Year` (via `GetReferencePeriod()`) | Não | Parte de `ReferencePeriod` |
| `ReferenceMonth` | `int` | `ReferencePeriod.Month` (via `GetReferencePeriod()`) | Não | Parte de `ReferencePeriod` |
| `DueDate` | `date` | `CalendarDate.GetValue()` (via `GetDueDate()`) | Não | Via `HasConversion` |
| `Status` | `int` | `OccurrenceStatus.GetValue()` (enum `OccurrenceStatusType`) | Não | Via `HasConversion` |
| `Name` | `nvarchar(200)` | `ExpenseName.GetValue()` (via `GetName()`) | Não | Snapshot do nome no momento da geração da ocorrência (já decidido no Domain) |
| `Category` | `int` | `ExpenseCategory.GetValue()` (via `GetCategory()`) | Não | Snapshot da categoria |
| `ExpectedAmount` | `decimal(18,2)` | `Money.GetValue()` (via `GetExpectedAmount()`) | Não | Snapshot do valor esperado |

`(ReferenceYear, ReferenceMonth)` juntos representam `ReferencePeriod`; não há um `ReferencePeriod.GetValue()` único (a classe expõe `Year`/`Month` como propriedades públicas, não via `GetValue()`), então o mapeamento usa duas colunas simples em vez de uma conversão de valor única.

## Relacionamento

```
RecurringExpense (1) ──────< (N) Occurrence
        Id                        RecurringExpenseId (FK, obrigatória)
```

- Uma `Occurrence` nunca existe sem uma `RecurringExpense` dona (FR-008) — FK `NOT NULL`.
- Uma `RecurringExpense` pode ter zero (Pausada, ou Ativa com início futuro) ou uma `Occurrence` no momento da criação (a do mês corrente); ocorrências adicionais (meses futuros) ficam para features futuras (fora de escopo).

## Reconstrução (leitura do banco)

Para satisfazer FR-009 sem reaplicar nem contornar validações de criação:

- `RecurringExpense` ganha um construtor privado de reconstrução que recebe `id`, todos os Value Objects já validados e a coleção de `Occurrence` já materializada, apenas atribuindo os campos — sem a lógica condicional de geração de ocorrência do construtor público.
- `Occurrence` ganha um construtor privado de reconstrução que recebe `id`, `status` e os demais Value Objects já validados, apenas atribuindo os campos — sem gerar novo `Guid` nem forçar `OccurrenceStatusType.Pending` como faz o construtor `internal` existente.
- Ambos os construtores privados são usados exclusivamente pelo EF Core (constructor binding), nunca chamados por código de aplicação ou de domínio.
- Os Value Objects continuam sendo reconstruídos através de seus construtores públicos existentes (ver `research.md` §1) — os valores já foram validados na escrita, então essa revalidação na leitura é redundante mas inofensiva, e não introduz nenhuma regra nova.

## Validation Rules

Nenhuma validação de negócio é implementada na camada de persistência (FR-010). As únicas restrições aplicadas em nível de schema são estruturais:
- `RecurringExpenses.Id` e `Occurrences.Id`: chave primária (`uniqueidentifier`, `NOT NULL`).
- `Occurrences.RecurringExpenseId`: chave estrangeira `NOT NULL` para `RecurringExpenses.Id`.
- Colunas correspondentes a Value Objects não-nuláveis: `NOT NULL` no schema, refletindo (não substituindo) a invariante já validada pelo Domain.

## State Transitions

Não aplicável nesta feature: nem `RecurringExpense` nem `Occurrence` têm métodos de transição de estado expostos ao contrato de repositório em escopo (`AddAsync`, `GetByIdAsync`, `GetActiveAsync` — apenas inclusão e leitura). Atualização de status (ex.: marcar ocorrência como paga) está fora de escopo (ver spec.md, Assumptions).
