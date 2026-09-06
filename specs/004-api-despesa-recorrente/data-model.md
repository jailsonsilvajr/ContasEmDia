# Phase 1 Data Model: API de Despesa Recorrente (Cadastro)

**Feature**: `004-api-despesa-recorrente` | **Date**: 2026-09-05

Esta feature não introduz nenhuma regra de negócio nem nenhum novo conceito
de domínio — apenas o transporte HTTP sobre o Use Case
`ICreateRecurringExpenseUseCase` já existente. As entidades abaixo são todas
tipos de transporte (API layer), sem comportamento além de carregar dados
(records/POCOs), conforme Princípio XII.

## `CreateRecurringExpenseDataRequest` (`/Requests`)

Corpo da requisição `POST /api/v1/recurring-expenses`. Espelha 1:1 os campos
primitivos de `CreateRecurringExpenseUseCaseInput` (Application). Validação
restrita a forma/tipo/presença — nenhuma regra de negócio.

| Campo | Tipo C# | Anotação | `ErrorMessage` (PT-BR) |
|---|---|---|---|
| `Name` | `string?` | `[Required]` | "Nome é obrigatório." |
| `Category` | `string?` | `[Required]` | "Categoria é obrigatória." |
| `MonthlyAmount` | `decimal?` | `[Required]` | "Valor previsto mensal é obrigatório." |
| `DueDay` | `int?` | `[Required]` | "Dia de vencimento é obrigatório." |
| `StartDate` | `string?` | `[Required]` | "Data de início é obrigatória." |
| `Frequency` | `string?` | `[Required]` | "Frequência é obrigatória." |
| `Status` | `string?` | `[Required]` | "Status é obrigatório." |
| `Note` | `string?` | — (opcional) | — |

`MonthlyAmount`/`DueDay` são anuláveis (`decimal?`/`int?`) apenas para que a
ausência do campo no JSON seja detectável por `[Required]` — o model
binding do ASP.NET Core não distingue "ausente" de "zero" em um tipo de
valor não anulável. A conversão para os tipos não anuláveis exigidos por
`CreateRecurringExpenseUseCaseInput` acontece somente dentro do mapeamento
`CreateRecurringExpenseDataRequestMapping`, após `ModelState` já ter
confirmado a presença dos campos.

**Relationships**: mapeado 1:1 para `CreateRecurringExpenseUseCaseInput`
(Application) via `CreateRecurringExpenseDataRequestMapping`.

## `CreateRecurringExpenseDataResponse` (`/Responses`)

Corpo de sucesso (`201`). Espelha os campos de sucesso de
`CreateRecurringExpenseUseCaseOutput`.

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
| `Occurrences` | `IReadOnlyCollection<OccurrenceDataResponse>` |

**Relationships**: mapeado a partir de `CreateRecurringExpenseUseCaseOutput`
(caminho de sucesso) via `CreateRecurringExpenseDataResponseMapping`.

## `OccurrenceDataResponse` (`/Responses`)

Item aninhado de `CreateRecurringExpenseDataResponse.Occurrences`, um por
ocorrência gerada (0 ou 1 nesta etapa).

| Campo | Tipo C# |
|---|---|
| `Id` | `Guid` |
| `ReferencePeriod` | `ReferencePeriodDataResponse` |
| `DueDate` | `DateOnly` |
| `Status` | `string` |
| `Name` | `string` |
| `Category` | `string` |
| `ExpectedAmount` | `decimal` |

**Relationships**: mapeado a partir de cada item de
`CreateRecurringExpenseUseCaseOutput.Occurrences` (`OccurrenceData`), com
`ReferenceYear`/`ReferenceMonth` (campos primitivos separados no Use Case)
reninhados em `ReferencePeriodDataResponse` pelo mesmo mapeamento
(`CreateRecurringExpenseDataResponseMapping`).

## `ReferencePeriodDataResponse` (`/Responses`)

Objeto aninhado dentro de `OccurrenceDataResponse`, espelhando a forma já
assumida pelo contrato de frontend (`referencePeriod: { year, month }`).

| Campo | Tipo C# |
|---|---|
| `Year` | `int` |
| `Month` | `int` |

## `ApiResponse<TData>` (`/Responses`)

Envelope de resposta único, compartilhado por toda a API (sucesso e erro,
deste endpoint e de qualquer futuro).

| Campo | Tipo C# | Presença |
|---|---|---|
| `Success` | `bool` | sempre |
| `Data` | `TData?` | apenas quando `Success == true` |
| `Errors` | `IReadOnlyCollection<ApiError>?` | apenas quando `Success == false` |

**State transitions**: não aplicável (tipo de transporte imutável, sem
ciclo de vida próprio).

## `ApiError` (`/Responses`)

Item de `ApiResponse<TData>.Errors`.

| Campo | Tipo C# | Observação |
|---|---|---|
| `Field` | `string?` | nome do campo em `camelCase`, igual ao da requisição; `null` quando o erro não é de um campo específico (ex.: erro `500` genérico) |
| `Message` | `string` | mensagem voltada ao usuário final, sempre em PT-BR |

## Fluxo de mapeamento (ponta a ponta)

```text
HTTP POST body (JSON)
  → CreateRecurringExpenseDataRequest        (model binding + [Required] PT-BR)
  → CreateRecurringExpenseDataRequestMapping
  → CreateRecurringExpenseUseCaseInput        (Application, já existente)
  → ICreateRecurringExpenseUseCase.ExecuteAsync
  → CreateRecurringExpenseUseCaseOutput       (Application, já existente)
      ├─ IsSuccess == true
      │    → CreateRecurringExpenseDataResponseMapping
      │    → CreateRecurringExpenseDataResponse
      │    → ApiResponse<CreateRecurringExpenseDataResponse>.Success(...)  → 201
      └─ IsSuccess == false
           → Output.Errors (FieldError[]) → ApiError[] (transposição direta)
           → ApiResponse<CreateRecurringExpenseDataResponse>.Failure(...)  → 400

ModelState inválido (forma/presença)
  → ApiBehaviorOptions.InvalidModelStateResponseFactory
  → ApiResponse<CreateRecurringExpenseDataResponse>.Failure(...)  → 400  (mesmo formato acima)

Exceção não tratada (ex.: banco indisponível)
  → ExceptionHandlingMiddleware
  → ApiResponse<CreateRecurringExpenseDataResponse>.Failure([erro genérico])  → 500
```
