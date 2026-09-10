using System.Globalization;
using ContasEmDia.Application.Ports;
using ContasEmDia.Application.UseCases.CreateRecurringExpense;
using ContasEmDia.Application.UseCases.GetRecurringExpenseById;
using ContasEmDia.Domain.Repositories;
using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Application.UseCases.UpdateRecurringExpense;

public sealed class UpdateRecurringExpenseUseCase : IUpdateRecurringExpenseUseCase
{
    private readonly IRepositoryManager _repositoryManager;
    private readonly ICurrentDateProvider _currentDateProvider;

    public UpdateRecurringExpenseUseCase(IRepositoryManager repositoryManager, ICurrentDateProvider currentDateProvider)
    {
        _repositoryManager = repositoryManager;
        _currentDateProvider = currentDateProvider;
    }

    public async Task<UpdateRecurringExpenseUseCaseOutput> ExecuteAsync(UpdateRecurringExpenseUseCaseInput input)
    {
        var recurringExpense = await _repositoryManager.RecurringExpenseRepository.GetByIdAsync(input.Id)
            ?? throw new KeyNotFoundException("Despesa recorrente não encontrada.");

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
            return UpdateRecurringExpenseUseCaseOutput.Failure(errors);
        }

        if (name!.GetValue() != recurringExpense.GetName().GetValue())
        {
            recurringExpense.Rename(name);
        }

        if (category!.GetValue() != recurringExpense.GetCategory().GetValue())
        {
            recurringExpense.ChangeCategory(category);
        }

        if (monthlyAmount!.GetValue() != recurringExpense.GetMonthlyAmount().GetValue())
        {
            recurringExpense.ChangeMonthlyAmount(monthlyAmount);
        }

        if (dueDay!.GetValue() != recurringExpense.GetDueDay().GetValue())
        {
            recurringExpense.ChangeDueDay(dueDay);
        }

        if (startDate!.GetValue() != recurringExpense.GetStartDate().GetValue())
        {
            recurringExpense.ChangeStartDate(startDate);
        }

        if (note.GetValue() != recurringExpense.GetNote().GetValue())
        {
            recurringExpense.ChangeNote(note);
        }

        var currentStatus = recurringExpense.GetStatus().GetValue();
        if (currentStatus != status!.GetValue())
        {
            if (status.GetValue() == RecurringExpenseStatusType.Paused)
            {
                recurringExpense.Pause();
            }
            else if (status.GetValue() == RecurringExpenseStatusType.Active)
            {
                recurringExpense.Reactivate(ReferencePeriod.FromDate(_currentDateProvider.GetCurrentDate()));
            }
        }

        await _repositoryManager.RecurringExpenseRepository.UpdateAsync(recurringExpense);

        var data = new RecurringExpenseData(
            recurringExpense.GetId(),
            recurringExpense.GetName().GetValue(),
            recurringExpense.GetCategory().GetValue().ToString(),
            recurringExpense.GetMonthlyAmount().GetValue(),
            recurringExpense.GetDueDay().GetValue(),
            recurringExpense.GetStartDate().GetValue(),
            recurringExpense.GetFrequency().GetValue().ToString(),
            recurringExpense.GetStatus().GetValue().ToString(),
            recurringExpense.GetNote().GetValue());

        return UpdateRecurringExpenseUseCaseOutput.Success(data);
    }
}
