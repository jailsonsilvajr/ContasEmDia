# Contract: `ContasEmDia.Application` Public API

**Feature**: `003-despesa-recorrente-application` | **Date**: 2026-09-04

Este projeto é uma biblioteca de classes backend, não um serviço HTTP — não tem endpoints. Seu "contrato" (caso "public APIs for libraries" do fluxo de planejamento) é a superfície pública que uma futura camada de API e os próprios testes desta feature podem depender. As assinaturas abaixo são normativas para a implementação; a ordem exata de parâmetros pode ser ajustada durante a implementação desde que o comportamento e os tipos sejam preservados.

Namespace raiz: `ContasEmDia.Application`. Este projeto referencia exclusivamente `ContasEmDia.Domain` (Princípio XI).

## Port: `ContasEmDia.Application.Ports.ICurrentDateProvider`

```csharp
public interface ICurrentDateProvider
{
    DateOnly GetCurrentDate();
}
```

Nenhuma implementação concreta faz parte desta feature — pertence a uma futura feature de Infrastructure (spec Assumptions). Os testes desta feature implementam este contrato com um dublê que devolve uma data fixa.

## Use Case: `ContasEmDia.Application.UseCases.CreateRecurringExpense`

```csharp
public interface ICreateRecurringExpenseUseCase
{
    Task<CreateRecurringExpenseUseCaseOutput> ExecuteAsync(CreateRecurringExpenseUseCaseInput input);
}

public sealed class CreateRecurringExpenseUseCase : ICreateRecurringExpenseUseCase
{
    public CreateRecurringExpenseUseCase(
        IRepositoryManager repositoryManager,
        ICurrentDateProvider currentDateProvider);

    public Task<CreateRecurringExpenseUseCaseOutput> ExecuteAsync(CreateRecurringExpenseUseCaseInput input);
}
```

```csharp
public sealed class CreateRecurringExpenseUseCaseInput
{
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required decimal MonthlyAmount { get; init; }
    public required int DueDay { get; init; }
    public required string StartDate { get; init; }   // "yyyy-MM-dd"
    public required string Frequency { get; init; }
    public required string Status { get; init; }
    public string? Note { get; init; }
}
```

```csharp
public sealed class CreateRecurringExpenseUseCaseOutput
{
    public bool IsSuccess { get; }

    // Populated only when IsSuccess == true; null otherwise.
    public Guid? Id { get; }
    public string? Name { get; }
    public string? Category { get; }
    public decimal? MonthlyAmount { get; }
    public int? DueDay { get; }
    public DateOnly? StartDate { get; }
    public string? Frequency { get; }
    public string? Status { get; }
    public string? Note { get; }
    public IReadOnlyCollection<OccurrenceData>? Occurrences { get; }   // 0 or 1 item

    // Populated only when IsSuccess == false; empty otherwise.
    public IReadOnlyCollection<FieldError> Errors { get; }

    public static CreateRecurringExpenseUseCaseOutput Success(/* dados da despesa criada + ocorrência, se houver */);
    public static CreateRecurringExpenseUseCaseOutput Failure(IReadOnlyCollection<FieldError> errors);
}

public sealed record OccurrenceData(
    Guid Id,
    int ReferenceYear,
    int ReferenceMonth,
    DateOnly DueDate,
    string Status,
    string Name,
    string Category,
    decimal ExpectedAmount);

public sealed record FieldError(string Field, string Message);
```

**Pre/postconditions**:
- `ExecuteAsync` nunca lança exceção para representar uma falha de validação de negócio — devolve `Output.Failure(...)` (FR-005, FR-006). Exceções lançadas pelo repositório (`IRepositoryManager.RecurringExpenseRepository.AddAsync`) por motivo não relacionado a validação propagam sem serem capturadas (FR-012).
- `Output.Success(...)` só é retornado depois que `AddAsync` retorna com sucesso — nenhuma despesa persistida parcialmente é reportada como sucesso (SC-004).
- `Errors` nunca é vazio quando `IsSuccess == false` (FR-005); os dados de sucesso nunca são populados quando `IsSuccess == false`, e vice-versa.
- A ordem dos `FieldError` na coleção não é normativa — a fronteira do Use Case não garante nem exige uma ordem específica de campos.

## Interface do Domain reaproveitada: `ContasEmDia.Domain.Repositories.IRepositoryManager` (NOVA — introduzida por esta feature)

```csharp
namespace ContasEmDia.Domain.Repositories;

public interface IRepositoryManager
{
    IRecurringExpenseRepository RecurringExpenseRepository { get; }
}
```

Implementada pela classe `ContasEmDia.Infrastructure.RepositoryManager` já existente (mudança de assinatura apenas — `: IRepositoryManager` adicionado à declaração da classe, sem alterar seu comportamento interno). O Use Case depende exclusivamente desta interface (via `Domain`), nunca da classe concreta em `Infrastructure` (FR-009, FR-015).

## Dependência reaproveitada do Domain (sem alteração de assinatura)

```csharp
// ContasEmDia.Domain.Repositories.IRecurringExpenseRepository — já existe, inalterada
public interface IRecurringExpenseRepository
{
    Task AddAsync(RecurringExpense recurringExpense);
    Task<RecurringExpense?> GetByIdAsync(Guid id);
    Task<IReadOnlyCollection<RecurringExpense>> GetActiveAsync();
}
```

Apenas `AddAsync` é usado por esta feature; `GetByIdAsync`/`GetActiveAsync` não são exercitados pelo Use Case de cadastro.

## Nota sobre mensagens de erro do Domain (FR-016)

As mensagens dos `ArgumentException` lançados pelos construtores de `ExpenseName`, `ExpenseCategory`, `Money`, `DueDay`, `Frequency`, `RecurringExpenseStatus` (e do aggregate `RecurringExpense`, onde aplicável) passam a estar em PT-BR como pré-requisito estrutural desta feature (ver `research.md` §6). O texto exato de cada mensagem não é normativo por este contrato — apenas a garantia de que está em PT-BR, é voltado ao usuário final, e é a mesma mensagem repassada tal como recebida no `FieldError.Message` correspondente (FR-006, SC-003). `CalendarDate`, `Note` e `ReferencePeriod` não precisam de revisão: `CalendarDate`/`Note` não lançam exceção hoje, e `ReferencePeriod` não é construída a partir de entrada do usuário neste Use Case (é derivada internamente via `ReferencePeriod.FromDate`, a partir de `ICurrentDateProvider`).
