using ContasEmDia.Api.Requests;
using ContasEmDia.Application.UseCases.UpdateRecurringExpense;

namespace ContasEmDia.Api.Mappings;

public static class UpdateRecurringExpenseDataRequestMapping
{
    public static UpdateRecurringExpenseUseCaseInput ToUseCaseInput(this UpdateRecurringExpenseDataRequest request, Guid id) =>
        new()
        {
            Id = id,
            Name = request.Name!,
            Category = request.Category!,
            MonthlyAmount = request.MonthlyAmount!.Value,
            DueDay = request.DueDay!.Value,
            StartDate = request.StartDate!,
            Status = request.Status!,
            Note = request.Note
        };
}
