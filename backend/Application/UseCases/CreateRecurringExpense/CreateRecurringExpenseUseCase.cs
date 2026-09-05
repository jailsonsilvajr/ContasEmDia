using System.Globalization;
using ContasEmDia.Application.Ports;
using ContasEmDia.Domain.Aggregates;
using ContasEmDia.Domain.Repositories;
using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Application.UseCases.CreateRecurringExpense;

public sealed class CreateRecurringExpenseUseCase : ICreateRecurringExpenseUseCase
{
    private readonly IRepositoryManager _repositoryManager;
    private readonly ICurrentDateProvider _currentDateProvider;

    public CreateRecurringExpenseUseCase(IRepositoryManager repositoryManager, ICurrentDateProvider currentDateProvider)
    {
        _repositoryManager = repositoryManager;
        _currentDateProvider = currentDateProvider;
    }

    public async Task<CreateRecurringExpenseUseCaseOutput> ExecuteAsync(CreateRecurringExpenseUseCaseInput input)
    {
        var errors = new List<FieldError>();

        ExpenseName? name = null;
        try
        {
            name = new ExpenseName(input.Name);
        }
        catch (ArgumentException ex)
        {
            errors.Add(new FieldError("name", ex.Message));
        }

        ExpenseCategory? category = null;
        if (Enum.TryParse<ExpenseCategoryType>(input.Category, out var categoryType))
        {
            try
            {
                category = new ExpenseCategory(categoryType);
            }
            catch (ArgumentException ex)
            {
                errors.Add(new FieldError("category", ex.Message));
            }
        }
        else
        {
            errors.Add(new FieldError("category", "Categoria inválida."));
        }

        Money? monthlyAmount = null;
        try
        {
            monthlyAmount = new Money(input.MonthlyAmount);
        }
        catch (ArgumentException ex)
        {
            errors.Add(new FieldError("monthlyAmount", ex.Message));
        }

        DueDay? dueDay = null;
        try
        {
            dueDay = new DueDay(input.DueDay);
        }
        catch (ArgumentException ex)
        {
            errors.Add(new FieldError("dueDay", ex.Message));
        }

        CalendarDate? startDate = null;
        if (DateOnly.TryParseExact(input.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDateValue))
        {
            startDate = new CalendarDate(startDateValue);
        }
        else
        {
            errors.Add(new FieldError("startDate", "Data de início inválida."));
        }

        Frequency? frequency = null;
        if (Enum.TryParse<FrequencyType>(input.Frequency, out var frequencyType))
        {
            try
            {
                frequency = new Frequency(frequencyType);
            }
            catch (ArgumentException ex)
            {
                errors.Add(new FieldError("frequency", ex.Message));
            }
        }
        else
        {
            errors.Add(new FieldError("frequency", "Frequência inválida."));
        }

        RecurringExpenseStatus? status = null;
        if (Enum.TryParse<RecurringExpenseStatusType>(input.Status, out var statusType))
        {
            try
            {
                status = new RecurringExpenseStatus(statusType);
            }
            catch (ArgumentException ex)
            {
                errors.Add(new FieldError("status", ex.Message));
            }
        }
        else
        {
            errors.Add(new FieldError("status", "Status inválido."));
        }

        var note = new Note(input.Note);

        if (errors.Count > 0)
        {
            return CreateRecurringExpenseUseCaseOutput.Failure(errors);
        }

        var currentReferencePeriod = ReferencePeriod.FromDate(_currentDateProvider.GetCurrentDate());

        var recurringExpense = new RecurringExpense(
            name!,
            category!,
            monthlyAmount!,
            dueDay!,
            startDate!,
            frequency!,
            status!,
            note,
            currentReferencePeriod);

        await _repositoryManager.RecurringExpenseRepository.AddAsync(recurringExpense);

        var occurrences = recurringExpense.GetOccurrences()
            .Select(occurrence => new OccurrenceData(
                occurrence.GetId(),
                occurrence.GetReferencePeriod().Year,
                occurrence.GetReferencePeriod().Month,
                occurrence.GetDueDate().GetValue(),
                occurrence.GetStatus().GetValue().ToString(),
                occurrence.GetName().GetValue(),
                occurrence.GetCategory().GetValue().ToString(),
                occurrence.GetExpectedAmount().GetValue()))
            .ToList();

        return CreateRecurringExpenseUseCaseOutput.Success(
            recurringExpense.GetId(),
            recurringExpense.GetName().GetValue(),
            recurringExpense.GetCategory().GetValue().ToString(),
            recurringExpense.GetMonthlyAmount().GetValue(),
            recurringExpense.GetDueDay().GetValue(),
            recurringExpense.GetStartDate().GetValue(),
            recurringExpense.GetFrequency().GetValue().ToString(),
            recurringExpense.GetStatus().GetValue().ToString(),
            recurringExpense.GetNote().GetValue(),
            occurrences);
    }
}
