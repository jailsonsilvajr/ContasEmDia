using ContasEmDia.Domain.Aggregates;
using ContasEmDia.Domain.Entities;
using ContasEmDia.Domain.ValueObjects;
using ContasEmDia.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ContasEmDia.Infrastructure.Tests.Repositories;

[Collection(nameof(PostgreSqlCollection))]
public sealed class RecurringExpenseRepositoryTests
{
    private readonly PostgreSqlContainerFixture _fixture;

    public RecurringExpenseRepositoryTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private static RecurringExpense CreateExpense(
        string name = "Aluguel",
        ExpenseCategoryType category = ExpenseCategoryType.Housing,
        decimal monthlyAmount = 1500m,
        int dueDay = 10,
        DateOnly? startDate = null,
        RecurringExpenseStatusType status = RecurringExpenseStatusType.Active,
        string? note = null,
        ReferencePeriod? currentReferencePeriod = null)
    {
        return new RecurringExpense(
            new ExpenseName(name),
            new ExpenseCategory(category),
            new Money(monthlyAmount),
            new DueDay(dueDay),
            new CalendarDate(startDate ?? new DateOnly(2026, 8, 1)),
            new Frequency(FrequencyType.Monthly),
            new RecurringExpenseStatus(status),
            new Note(note),
            currentReferencePeriod ?? new ReferencePeriod(2026, 8));
    }

    [Fact]
    public async Task AddAsync_ActiveExpenseWithCurrentOccurrence_PersistsExpenseAndOccurrenceInSingleWrite()
    {
        var expense = CreateExpense();

        await using var context = _fixture.CreateContext();
        var repository = new RecurringExpenseRepository(context);

        await repository.AddAsync(expense);

        await using var verifyContext = _fixture.CreateContext();
        var savedExpense = await verifyContext.RecurringExpenses
            .Include("_occurrences")
            .FirstOrDefaultAsync(e => EF.Property<Guid>(e, "_id") == expense.GetId());

        Assert.NotNull(savedExpense);
        Assert.Single(savedExpense.GetOccurrences());
    }

    [Fact]
    public async Task AddAsync_PausedExpense_PersistsOnlyExpenseWithoutOccurrence()
    {
        var expense = CreateExpense(status: RecurringExpenseStatusType.Paused);

        await using var context = _fixture.CreateContext();
        var repository = new RecurringExpenseRepository(context);

        await repository.AddAsync(expense);

        await using var verifyContext = _fixture.CreateContext();
        var savedExpense = await verifyContext.RecurringExpenses
            .Include("_occurrences")
            .FirstOrDefaultAsync(e => EF.Property<Guid>(e, "_id") == expense.GetId());

        Assert.NotNull(savedExpense);
        Assert.Empty(savedExpense.GetOccurrences());
    }

    [Fact]
    public async Task AddAsync_WriteFailure_PersistsNeitherExpenseNorOccurrence()
    {
        var expense = CreateExpense();
        var occurrence = expense.GetOccurrences().Single();

        await using var context = _fixture.CreateContext();
        context.RecurringExpenses.Add(expense);
        context.Entry(occurrence).Property("RecurringExpenseId").CurrentValue = Guid.NewGuid();

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        await using var verifyContext = _fixture.CreateContext();
        var expensePersisted = await verifyContext.RecurringExpenses
            .AnyAsync(e => EF.Property<Guid>(e, "_id") == expense.GetId());
        var occurrencePersisted = await verifyContext.Set<Occurrence>()
            .AnyAsync(o => EF.Property<Guid>(o, "_id") == occurrence.GetId());

        Assert.False(expensePersisted);
        Assert.False(occurrencePersisted);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingIdWithOccurrences_ReturnsExpenseWithOccurrencesIntact()
    {
        var expense = CreateExpense();

        await using (var context = _fixture.CreateContext())
        {
            var repository = new RecurringExpenseRepository(context);
            await repository.AddAsync(expense);
        }

        await using var queryContext = _fixture.CreateContext();
        var repositoryUnderTest = new RecurringExpenseRepository(queryContext);

        var retrieved = await repositoryUnderTest.GetByIdAsync(expense.GetId());

        Assert.NotNull(retrieved);
        Assert.Equal(expense.GetId(), retrieved.GetId());
        Assert.Equal(expense.GetName().GetValue(), retrieved.GetName().GetValue());
        Assert.Single(retrieved.GetOccurrences());
        Assert.Equal(
            expense.GetOccurrences().Single().GetId(),
            retrieved.GetOccurrences().Single().GetId());
    }

    [Fact]
    public async Task GetByIdAsync_ExpenseWithNullNote_ReturnsNonNullNoteValueObjectWithNullValue()
    {
        var expense = CreateExpense(note: null);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new RecurringExpenseRepository(context);
            await repository.AddAsync(expense);
        }

        await using var queryContext = _fixture.CreateContext();
        var repositoryUnderTest = new RecurringExpenseRepository(queryContext);

        var retrieved = await repositoryUnderTest.GetByIdAsync(expense.GetId());

        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved.GetNote());
        Assert.Null(retrieved.GetNote().GetValue());
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNullWithoutThrowing()
    {
        await using var context = _fixture.CreateContext();
        var repository = new RecurringExpenseRepository(context);

        var retrieved = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetActiveAsync_MixedStatuses_ReturnsOnlyActiveExpensesWithOccurrences()
    {
        var activeExpense = CreateExpense(status: RecurringExpenseStatusType.Active);
        var pausedExpense = CreateExpense(status: RecurringExpenseStatusType.Paused);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new RecurringExpenseRepository(context);
            await repository.AddAsync(activeExpense);
            await repository.AddAsync(pausedExpense);
        }

        await using var queryContext = _fixture.CreateContext();
        var repositoryUnderTest = new RecurringExpenseRepository(queryContext);

        var active = await repositoryUnderTest.GetActiveAsync();

        var activeIds = active.Select(e => e.GetId()).ToList();
        Assert.Contains(activeExpense.GetId(), activeIds);
        Assert.DoesNotContain(pausedExpense.GetId(), activeIds);

        var returnedActiveExpense = active.Single(e => e.GetId() == activeExpense.GetId());
        Assert.Single(returnedActiveExpense.GetOccurrences());
    }

    [Fact]
    public async Task GetActiveAsync_NoActiveExpensesPersisted_ReturnsEmptyList()
    {
        var pausedExpense = CreateExpense(status: RecurringExpenseStatusType.Paused);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new RecurringExpenseRepository(context);
            await repository.AddAsync(pausedExpense);
        }

        await using var queryContext = _fixture.CreateContext();
        var repositoryUnderTest = new RecurringExpenseRepository(queryContext);

        var active = await repositoryUnderTest.GetActiveAsync();

        Assert.DoesNotContain(pausedExpense.GetId(), active.Select(e => e.GetId()));
    }

    [Fact]
    public async Task GetByReferencePeriodAsync_ActiveAndPausedExpensesWithOccurrenceInPeriod_ReturnsBoth()
    {
        var activeExpense = CreateExpense(
            status: RecurringExpenseStatusType.Active,
            startDate: new DateOnly(2026, 8, 1),
            currentReferencePeriod: new ReferencePeriod(2026, 8));

        // An expense only generates an occurrence while Active at construction
        // time; RF20 requires an expense later paused to keep showing occurrences
        // it already generated. There is no public mutation for status yet, so
        // we simulate "was active, generated an occurrence, then got paused" via
        // reflection on the private field, matching the pattern already used for
        // OccurrenceTests's Paid-state setup.
        var pausedExpense = CreateExpense(
            status: RecurringExpenseStatusType.Active,
            startDate: new DateOnly(2026, 8, 1),
            currentReferencePeriod: new ReferencePeriod(2026, 8));
        typeof(RecurringExpense)
            .GetField("_status", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(pausedExpense, new RecurringExpenseStatus(RecurringExpenseStatusType.Paused));

        await using (var context = _fixture.CreateContext())
        {
            var repository = new RecurringExpenseRepository(context);
            await repository.AddAsync(activeExpense);
            await repository.AddAsync(pausedExpense);
        }

        await using var queryContext = _fixture.CreateContext();
        var repositoryUnderTest = new RecurringExpenseRepository(queryContext);

        var matching = await repositoryUnderTest.GetByReferencePeriodAsync(new ReferencePeriod(2026, 8));

        var matchingIds = matching.Select(e => e.GetId()).ToList();
        Assert.Contains(activeExpense.GetId(), matchingIds);
        Assert.Contains(pausedExpense.GetId(), matchingIds);
    }

    [Fact]
    public async Task GetByReferencePeriodAsync_PeriodWithNoOccurrences_ReturnsEmptyCollection()
    {
        var expense = CreateExpense(
            status: RecurringExpenseStatusType.Active,
            startDate: new DateOnly(2026, 8, 1),
            currentReferencePeriod: new ReferencePeriod(2026, 8));

        await using (var context = _fixture.CreateContext())
        {
            var repository = new RecurringExpenseRepository(context);
            await repository.AddAsync(expense);
        }

        await using var queryContext = _fixture.CreateContext();
        var repositoryUnderTest = new RecurringExpenseRepository(queryContext);

        var matching = await repositoryUnderTest.GetByReferencePeriodAsync(new ReferencePeriod(2020, 1));

        Assert.Empty(matching);
    }

    [Fact]
    public async Task GetByOccurrenceIdAsync_ExistingOccurrenceId_ReturnsOwningRecurringExpense()
    {
        var expense = CreateExpense();
        var occurrence = expense.GetOccurrences().Single();

        await using (var context = _fixture.CreateContext())
        {
            var repository = new RecurringExpenseRepository(context);
            await repository.AddAsync(expense);
        }

        await using var queryContext = _fixture.CreateContext();
        var repositoryUnderTest = new RecurringExpenseRepository(queryContext);

        var retrieved = await repositoryUnderTest.GetByOccurrenceIdAsync(occurrence.GetId());

        Assert.NotNull(retrieved);
        Assert.Equal(expense.GetId(), retrieved.GetId());
    }

    [Fact]
    public async Task GetByOccurrenceIdAsync_NonExistentOccurrenceId_ReturnsNull()
    {
        await using var context = _fixture.CreateContext();
        var repository = new RecurringExpenseRepository(context);

        var retrieved = await repository.GetByOccurrenceIdAsync(Guid.NewGuid());

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task UpdateAsync_MutatedTrackedAggregate_PersistsMutation()
    {
        var expense = CreateExpense();
        var occurrence = expense.GetOccurrences().Single();

        await using (var context = _fixture.CreateContext())
        {
            var repository = new RecurringExpenseRepository(context);
            await repository.AddAsync(expense);
        }

        await using var updateContext = _fixture.CreateContext();
        var updateRepository = new RecurringExpenseRepository(updateContext);
        var trackedExpense = await updateRepository.GetByIdAsync(expense.GetId());
        var trackedOccurrence = trackedExpense!.FindOccurrence(occurrence.GetId())!;

        var paidAmount = new Money(1500m);
        var paymentDate = new CalendarDate(new DateOnly(2026, 8, 5));
        trackedExpense.MarkOccurrenceAsPaid(trackedOccurrence.GetId(), paidAmount, paymentDate);

        await updateRepository.UpdateAsync(trackedExpense);

        await using var verifyContext = _fixture.CreateContext();
        var verifyRepository = new RecurringExpenseRepository(verifyContext);
        var verified = await verifyRepository.GetByIdAsync(expense.GetId());
        var verifiedOccurrence = verified!.FindOccurrence(occurrence.GetId())!;

        Assert.Equal(paidAmount.GetValue(), verifiedOccurrence.GetPaidAmount()!.GetValue());
        Assert.Equal(paymentDate.GetValue(), verifiedOccurrence.GetPaymentDate()!.GetValue());
    }

    [Fact]
    public async Task UpdateAsync_ReactivatedAggregateAddsNewOccurrence_PersistsTheNewOccurrenceAsAnInsert()
    {
        var expense = CreateExpense(status: RecurringExpenseStatusType.Paused);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new RecurringExpenseRepository(context);
            await repository.AddAsync(expense);
        }

        await using var updateContext = _fixture.CreateContext();
        var updateRepository = new RecurringExpenseRepository(updateContext);
        var trackedExpense = await updateRepository.GetByIdAsync(expense.GetId());

        trackedExpense!.Reactivate(new ReferencePeriod(2026, 8));
        await updateRepository.UpdateAsync(trackedExpense);

        await using var verifyContext = _fixture.CreateContext();
        var verifyRepository = new RecurringExpenseRepository(verifyContext);
        var verified = await verifyRepository.GetByIdAsync(expense.GetId());

        Assert.Equal(RecurringExpenseStatusType.Active, verified!.GetStatus().GetValue());
        Assert.Single(verified.GetOccurrences());
    }
}
