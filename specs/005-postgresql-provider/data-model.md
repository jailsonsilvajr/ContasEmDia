# Data Model: Provider de Banco de Dados PostgreSQL

Esta feature é uma troca de provider de persistência — ela não adiciona,
remove ou altera nenhum Aggregate, Entity ou Value Object do domínio. O
modelo de domínio abaixo é o mesmo já existente (ver
`specs/002-despesa-recorrente-infrastructure/data-model.md`); o que muda é
exclusivamente como cada campo é mapeado para uma coluna PostgreSQL na
camada `Infrastructure/Configs`.

## Entidades persistidas (inalteradas)

### RecurringExpense (Aggregate Root) — tabela `RecurringExpenses`

| Campo (Value Object) | Coluna | Tipo PostgreSQL (via EF Core) | Observação |
|---|---|---|---|
| `_id` (Guid) | `Id` | `uuid` | Chave primária. Mapeamento nativo do provider Npgsql, sem `HasColumnType`. |
| `_name` (ExpenseName) | `Name` | `character varying(200)` | Via `HasMaxLength(200)`, agnóstico de provider — sem mudança de config. |
| `_category` (ExpenseCategory) | `Category` | `integer` | Enum convertido para `int`. |
| `_monthlyAmount` (Money) | `MonthlyAmount` | `numeric(18,2)` | Trocar `.HasColumnType("decimal(18,2)")` por `.HasPrecision(18, 2)` (ver research.md). |
| `_dueDay` (DueDay) | `DueDay` | `integer` | Sem mudança. |
| `_startDate` (CalendarDate) | `StartDate` | `date` | `.HasColumnType("date")` continua válido no Npgsql. |
| `_frequency` (Frequency) | `Frequency` | `integer` | Enum convertido para `int`. |
| `_status` (RecurringExpenseStatus) | `Status` | `integer` | Enum convertido para `int`. |
| `_note` (Note, opcional) | `Note` | `character varying(1000)` (nullable) | Via `HasMaxLength(1000)` + `IsRequired(false)`, sem mudança. |
| `_occurrences` (coleção) | FK `RecurringExpenseId` em `Occurrences` | `uuid` | Relacionamento 1:N, `OnDelete: Cascade`, sem mudança estrutural. |

### Occurrence (Entity, acessível apenas via RecurringExpense) — tabela `Occurrences`

| Campo (Value Object) | Coluna | Tipo PostgreSQL (via EF Core) | Observação |
|---|---|---|---|
| `_id` (Guid) | `Id` | `uuid` | Chave primária. |
| `RecurringExpenseId` (Guid, sombra) | `RecurringExpenseId` | `uuid` | FK obrigatória, indexada (`IX_Occurrences_RecurringExpenseId`). |
| `_referencePeriod` (ReferencePeriod, complex type) | `ReferenceYear`, `ReferenceMonth` | `integer`, `integer` | Complex property, sem mudança. |
| `_dueDate` (CalendarDate) | `DueDate` | `date` | Sem mudança. |
| `_status` (OccurrenceStatus) | `Status` | `integer` | Enum convertido para `int`. |
| `_name` (ExpenseName) | `Name` | `character varying(200)` | Sem mudança. |
| `_category` (ExpenseCategory) | `Category` | `integer` | Sem mudança. |
| `_expectedAmount` (Money) | `ExpectedAmount` | `numeric(18,2)` | Trocar `.HasColumnType("decimal(18,2)")` por `.HasPrecision(18, 2)` (ver research.md). |

## Regras de validação

Todas as invariantes (obrigatoriedade, tamanho máximo, faixa de valores)
continuam implementadas exclusivamente no Domain (Principle VI); nenhuma
regra de negócio é adicionada, removida ou alterada por esta troca de
provider. As únicas mudanças em `Infrastructure/Configs` são de
mapeamento físico (tipo de coluna), não de validação.

## Transições de estado

Não aplicável — nenhuma máquina de estados do domínio é afetada por esta
troca de provider.

## Migração de schema

A migração inicial (`InitialCreate`) é regerada do zero para o provider
Npgsql, substituindo a migração equivalente gerada para SQL Server (ver
research.md, seção "Decisão: Migração de schema"). O schema lógico
resultante (tabelas, colunas, chaves, índices, cascata de exclusão) é
idêntico ao já existente — apenas os tipos físicos de coluna mudam para
seus equivalentes PostgreSQL listados acima.
