using ContasEmDia.Api.Responses;
using ContasEmDia.Application.UseCases.CreateRecurringExpense;

namespace ContasEmDia.Api.Mappings;

public static class CreateRecurringExpenseDataResponseMapping
{
    public static CreateRecurringExpenseDataResponse ToDataResponse(this CreateRecurringExpenseUseCaseOutput output) =>
        new(
            Id: output.Id!.Value,
            Name: output.Name!,
            Category: output.Category!,
            MonthlyAmount: output.MonthlyAmount!.Value,
            DueDay: output.DueDay!.Value,
            StartDate: output.StartDate!.Value,
            Frequency: output.Frequency!,
            Status: output.Status!,
            Note: output.Note,
            Occurrences: output.Occurrences!.Select(ToOccurrenceDataResponse).ToList());

    private static OccurrenceDataResponse ToOccurrenceDataResponse(OccurrenceData occurrence) =>
        new(
            Id: occurrence.Id,
            ReferencePeriod: new ReferencePeriodDataResponse(occurrence.ReferenceYear, occurrence.ReferenceMonth),
            DueDate: occurrence.DueDate,
            Status: occurrence.Status,
            Name: occurrence.Name,
            Category: occurrence.Category,
            ExpectedAmount: occurrence.ExpectedAmount);

    public static IReadOnlyCollection<ApiError> ToApiErrors(this IReadOnlyCollection<FieldError> errors) =>
        errors.Select(error => new ApiError(error.Field, error.Message)).ToList();
}
