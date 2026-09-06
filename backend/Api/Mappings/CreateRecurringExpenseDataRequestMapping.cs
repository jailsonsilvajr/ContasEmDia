using ContasEmDia.Api.Requests;
using ContasEmDia.Application.UseCases.CreateRecurringExpense;

namespace ContasEmDia.Api.Mappings;

public static class CreateRecurringExpenseDataRequestMapping
{
    public static CreateRecurringExpenseUseCaseInput ToUseCaseInput(this CreateRecurringExpenseDataRequest request) =>
        new()
        {
            Name = request.Name!,
            Category = request.Category!,
            MonthlyAmount = request.MonthlyAmount!.Value,
            DueDay = request.DueDay!.Value,
            StartDate = request.StartDate!,
            Frequency = request.Frequency!,
            Status = request.Status!,
            Note = request.Note
        };
}
