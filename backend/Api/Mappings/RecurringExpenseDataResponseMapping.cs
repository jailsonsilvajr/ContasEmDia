using ContasEmDia.Api.Responses;
using ContasEmDia.Application.UseCases.GetRecurringExpenseById;

namespace ContasEmDia.Api.Mappings;

public static class RecurringExpenseDataResponseMapping
{
    public static RecurringExpenseDataResponse ToDataResponse(this RecurringExpenseData data) =>
        new(
            Id: data.Id,
            Name: data.Name,
            Category: data.Category,
            MonthlyAmount: data.MonthlyAmount,
            DueDay: data.DueDay,
            StartDate: data.StartDate,
            Frequency: data.Frequency,
            Status: data.Status,
            Note: data.Note);
}
