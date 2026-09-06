# Phase 1 Data Model: Application de Despesa Recorrente (Cadastro)

Este documento descreve as estruturas de dados do Use Case `CreateRecurringExpenseUseCase` (Input, Output e tipos de apoio) e as duas interfaces novas introduzidas por esta feature (`ICurrentDateProvider` em Application, `IRepositoryManager` em Domain). Nenhuma regra de negócio é definida aqui — apenas a forma dos dados que atravessam a fronteira do Use Case (FR-001, FR-013).

## `CreateRecurringExpenseUseCaseInput`

Espelha 1:1 os campos primitivos do corpo do `POST /api/recurring-expenses` já documentado (ver `specs/002-cadastro-despesa-recorrente/contracts/api-contract.md`).

| Campo | Tipo primitivo | Obrigatório | Value Object de destino (Domain) |
|---|---|---|---|
| `Name` | `string` | Sim | `ExpenseName` |
| `Category` | `string` | Sim | `ExpenseCategory` (via `ExpenseCategoryType`) |
| `MonthlyAmount` | `decimal` | Sim | `Money` |
| `DueDay` | `int` | Sim | `DueDay` |
| `StartDate` | `string` (`yyyy-MM-dd`) | Sim | `CalendarDate` |
| `Frequency` | `string` | Sim | `Frequency` (via `FrequencyType`) |
| `Status` | `string` | Sim | `RecurringExpenseStatus` (via `RecurringExpenseStatusType`) |
| `Note` | `string?` | Não | `Note` |

Nenhum campo é um Value Object do Domain — quem preenche este Input a partir de um corpo HTTP é uma futura camada de API, fora de escopo (spec Assumptions).

## `CreateRecurringExpenseUseCaseOutput`

Representa duas possibilidades mutuamente exclusivas — nunca ambas presentes ao mesmo tempo:

| Membro | Tipo | Presente quando |
|---|---|---|
| `IsSuccess` | `bool` | Sempre |
| Dados de sucesso (despesa criada + ocorrência, se houver) | ver "Dados de sucesso" abaixo | `IsSuccess == true` |
| `Errors` | `IReadOnlyCollection<FieldError>` | `IsSuccess == false` (não vazia — FR-005) |

### Dados de sucesso

Todos obtidos exclusivamente através dos métodos de leitura já expostos por `RecurringExpense`/`Occurrence` (FR-010) — nunca por acesso a estado interno.

| Campo | Tipo primitivo | Origem (Domain) |
|---|---|---|
| `Id` | `Guid` | `RecurringExpense.GetId()` |
| `Name` | `string` | `RecurringExpense.GetName().GetValue()` |
| `Category` | `string` | `RecurringExpense.GetCategory().GetValue().ToString()` |
| `MonthlyAmount` | `decimal` | `RecurringExpense.GetMonthlyAmount().GetValue()` |
| `DueDay` | `int` | `RecurringExpense.GetDueDay().GetValue()` |
| `StartDate` | `DateOnly` | `RecurringExpense.GetStartDate().GetValue()` |
| `Frequency` | `string` | `RecurringExpense.GetFrequency().GetValue().ToString()` |
| `Status` | `string` | `RecurringExpense.GetStatus().GetValue().ToString()` |
| `Note` | `string?` | `RecurringExpense.GetNote().GetValue()` |
| `Occurrences` | `IReadOnlyCollection<OccurrenceData>` | `RecurringExpense.GetOccurrences()` — 0 ou 1 item (FR-011) |

`OccurrenceData` (um item por `Occurrence`, no máximo um nesta feature):

| Campo | Tipo primitivo | Origem (Domain) |
|---|---|---|
| `Id` | `Guid` | `Occurrence.GetId()` |
| `ReferenceYear` | `int` | `Occurrence.GetReferencePeriod().Year` |
| `ReferenceMonth` | `int` | `Occurrence.GetReferencePeriod().Month` |
| `DueDate` | `DateOnly` | `Occurrence.GetDueDate().GetValue()` |
| `Status` | `string` | `Occurrence.GetStatus().GetValue().ToString()` |
| `Name` | `string` | `Occurrence.GetName().GetValue()` |
| `Category` | `string` | `Occurrence.GetCategory().GetValue().ToString()` |
| `ExpectedAmount` | `decimal` | `Occurrence.GetExpectedAmount().GetValue()` |

### `FieldError` (tipo de apoio, não é o Output em si)

| Campo | Tipo | Descrição |
|---|---|---|
| `Field` | `string` | Nome do campo do Input que falhou (`name`, `category`, `monthlyAmount`, `dueDay`, `startDate`, `frequency`, `status`) — mesmos nomes já usados no contrato de API (`specs/002-cadastro-despesa-recorrente/contracts/api-contract.md`) |
| `Message` | `string` | Mensagem PT-BR voltada ao usuário — repassada tal como recebida do Value Object/aggregate do Domain (FR-006), ou produzida pela própria Application apenas nos dois casos descritos em `research.md` §4–5 (categoria/frequência/status não reconhecidos antes de tentar construir o Value Object; texto de `startDate` malformado ou calendarialmente inválido) |

`Note` nunca gera `FieldError` — seu Value Object não valida nada (`Note(string? value)` sempre sucede).

## `ICurrentDateProvider` (novo port, `Application/Ports`)

| Membro | Assinatura | Descrição |
|---|---|---|
| `GetCurrentDate` | `DateOnly GetCurrentDate()` | Retorna a data atual do sistema. Nenhuma implementação concreta faz parte desta feature (ver `research.md` §2) — apenas a interface e dublês de teste. |

## `IRepositoryManager` (nova interface, `Domain/Repositories`)

| Membro | Assinatura | Descrição |
|---|---|---|
| `RecurringExpenseRepository` | `IRecurringExpenseRepository RecurringExpenseRepository { get; }` | Ponto único de acesso ao repositório de despesas recorrentes, implementado pela classe concreta `RepositoryManager` já existente na Infrastructure (ver `research.md` §1). |

## Fluxo de dados do Use Case (não é uma máquina de estados do Domain — apenas o pipeline de orquestração)

```
CreateRecurringExpenseUseCaseInput
        │
        ▼
[1] Converter cada campo → Value Object, acumulando FieldError sem interromper (FR-002, FR-005)
        │
        ├─ houve algum FieldError? ──► Output de falha (Errors), sem chamar repositório nem ICurrentDateProvider
        │
        ▼ (todos os campos convertidos com sucesso)
[2] ICurrentDateProvider.GetCurrentDate() → ReferencePeriod.FromDate(...) (FR-007)
        │
        ▼
[3] new RecurringExpense(...) — construtor público apenas (FR-008)
        │
        ▼
[4] repositoryManager.RecurringExpenseRepository.AddAsync(...) (FR-009)
        │  (falha não relacionada a validação propaga como exceção — FR-012, não vira Output de falha)
        ▼
[5] Montar Output de sucesso a partir dos métodos de leitura do aggregate/entidade (FR-010, FR-011)
```

## Validation Rules

Nenhuma regra de negócio nova (todas já fechadas no Domain — spec Assumptions). O que esta camada decide é apenas *quando tentar* construir cada Value Object e *como agregar* as falhas já sinalizadas pelo Domain:

- Todos os 7 campos obrigatórios são convertidos, mesmo que um anterior já tenha falhado (FR-005) — sem curto-circuito.
- `Category`/`Frequency`/`Status`: `Enum.TryParse` explícito antes de construir o Value Object (ver `research.md` §4).
- `StartDate`: `DateOnly.TryParseExact` com formato exato `yyyy-MM-dd`, cultura invariante, antes de construir `CalendarDate` (ver `research.md` §5).
- `Note` é sempre convertida com sucesso (não participa da agregação de erros).
- Nenhum Value Object é construído com um valor "provisório" ou "default" apenas para satisfazer o construtor do aggregate quando outro campo falhou — se houver qualquer `FieldError`, o construtor de `RecurringExpense` nunca é chamado.

## State Transitions

Não aplicável — este Use Case cobre exclusivamente criação (`AddAsync`); nenhuma transição de estado de uma despesa/ocorrência já existente está em escopo (spec Assumptions).
