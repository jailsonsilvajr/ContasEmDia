using ContasEmDia.Api.Responses;
using ContasEmDia.Application.UseCases.DeleteRecurringExpense;

namespace ContasEmDia.Api.Mappings;

public static class DeleteRecurringExpenseDataResponseMapping
{
    public static DeleteRecurringExpenseDataResponse ToDataResponse(this DeleteRecurringExpenseData data) =>
        new(data.RecurringExpenseId, data.Scope);
}
