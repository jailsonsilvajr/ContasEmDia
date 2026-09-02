# Contract: Repository Access (Infrastructure Layer)

Esta feature não expõe nenhuma API HTTP (fora de escopo — ver spec.md, Assumptions). O "contrato" desta camada é o ponto de acesso que a camada de aplicação (futura) usará para chegar à persistência: o `RepositoryManager` e a implementação de `IRecurringExpenseRepository`, ambos já com formato definido no Domain/refinamento de origem.

## `IRecurringExpenseRepository` (já definido em `backend/Domain/Repositories/IRecurringExpenseRepository.cs`)

Esta feature implementa, sem alterar a assinatura:

```csharp
public interface IRecurringExpenseRepository
{
    Task AddAsync(RecurringExpense recurringExpense);

    Task<RecurringExpense?> GetByIdAsync(Guid id);

    Task<IReadOnlyCollection<RecurringExpense>> GetActiveAsync();
}
```

| Método | Pré-condição | Pós-condição | Erros esperados |
|---|---|---|---|
| `AddAsync(RecurringExpense)` | `recurringExpense` já validado pelo Domain (construtor público) | Persiste a despesa e, se presente, sua ocorrência do mês corrente em uma única chamada a `SaveChangesAsync` (FR-001); em caso de falha de escrita, nada fica parcialmente persistido (FR-002) | Exceção propagada do EF Core/SQL Server em caso de falha de escrita (ex.: `DbUpdateException`); não há tratamento/mapeamento de erro nesta camada — decisão de tratamento fica para a camada de aplicação (fora de escopo) |
| `GetByIdAsync(Guid id)` | `id` de uma despesa possivelmente existente | Retorna a `RecurringExpense` reconstruída com todas as suas `Occurrence`s (FR-004), ou `null` se não existir (FR-005) | Nenhum (retorno `null`, nunca exceção, para "não encontrado") |
| `GetActiveAsync()` | Nenhuma | Retorna todas as `RecurringExpense` com status Ativa, cada uma com suas `Occurrence`s (FR-006); lista vazia se nenhuma (FR-007) | Nenhum (lista vazia, nunca exceção, para "nenhum resultado") |

## `RecurringExpenseRepository` (nova implementação, `backend/Infrastructure/Repositories/RecurringExpenseRepository.cs`)

Implementação concreta de `IRecurringExpenseRepository` sobre `ContasEmDiaDbContext`, sem nenhuma lógica de negócio adicional (FR-010) — apenas tradução para operações EF Core (`Add`, `SaveChangesAsync`, `FirstOrDefaultAsync` com `Include(Occurrences)`, `Where(Status == Active)` com `Include`).

## `RepositoryManager` (novo, `backend/Infrastructure/RepositoryManager.cs`)

Ponto único de acesso aos repositórios da camada Infrastructure (FR-012), conforme Princípio VII da constituição:

```csharp
public sealed class RepositoryManager
{
    private readonly Lazy<IRecurringExpenseRepository> _recurringExpenseRepository;

    public RepositoryManager(ContasEmDiaDbContext context)
    {
        _recurringExpenseRepository = new Lazy<IRecurringExpenseRepository>(
            () => new RecurringExpenseRepository(context));
    }

    public IRecurringExpenseRepository RecurringExpenseRepository => _recurringExpenseRepository.Value;
}
```

- A instanciação de `RecurringExpenseRepository` só ocorre no primeiro acesso à propriedade (`Lazy<T>`).
- Consumidores futuros (camada de aplicação/API, fora de escopo) obtêm o repositório exclusivamente através desta classe — nunca instanciando `RecurringExpenseRepository` diretamente.

## `ContasEmDiaDbContext` (novo, `backend/Infrastructure/Contexts/ContasEmDiaDbContext.cs`)

```csharp
public sealed class ContasEmDiaDbContext : DbContext
{
    public ContasEmDiaDbContext(DbContextOptions<ContasEmDiaDbContext> options)
        : base(options)
    {
    }

    public DbSet<RecurringExpense> RecurringExpenses => Set<RecurringExpense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContasEmDiaDbContext).Assembly);
    }
}
```

- Recebe `DbContextOptions<ContasEmDiaDbContext>` já configuradas externamente (connection string vinda de configuração externa, FR-011) — nunca constrói sua própria connection string.
- Expõe apenas `RecurringExpenses` como `DbSet<T>` — `Occurrence` não tem `DbSet` próprio, é alcançada só via navegação (consistente com o refinamento de origem: `Occurrence` não é aggregate independente).
