namespace ContasEmDia.Application.UseCases.CreateRecurringExpense;

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

public sealed class CreateRecurringExpenseUseCaseOutput
{
    private CreateRecurringExpenseUseCaseOutput(
        bool isSuccess,
        Guid? id,
        string? name,
        string? category,
        decimal? monthlyAmount,
        int? dueDay,
        DateOnly? startDate,
        string? frequency,
        string? status,
        string? note,
        IReadOnlyCollection<OccurrenceData>? occurrences,
        IReadOnlyCollection<FieldError> errors)
    {
        IsSuccess = isSuccess;
        Id = id;
        Name = name;
        Category = category;
        MonthlyAmount = monthlyAmount;
        DueDay = dueDay;
        StartDate = startDate;
        Frequency = frequency;
        Status = status;
        Note = note;
        Occurrences = occurrences;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public Guid? Id { get; }

    public string? Name { get; }

    public string? Category { get; }

    public decimal? MonthlyAmount { get; }

    public int? DueDay { get; }

    public DateOnly? StartDate { get; }

    public string? Frequency { get; }

    public string? Status { get; }

    public string? Note { get; }

    public IReadOnlyCollection<OccurrenceData>? Occurrences { get; }

    public IReadOnlyCollection<FieldError> Errors { get; }

    public static CreateRecurringExpenseUseCaseOutput Success(
        Guid id,
        string name,
        string category,
        decimal monthlyAmount,
        int dueDay,
        DateOnly startDate,
        string frequency,
        string status,
        string? note,
        IReadOnlyCollection<OccurrenceData> occurrences) =>
        new(
            isSuccess: true,
            id: id,
            name: name,
            category: category,
            monthlyAmount: monthlyAmount,
            dueDay: dueDay,
            startDate: startDate,
            frequency: frequency,
            status: status,
            note: note,
            occurrences: occurrences,
            errors: []);

    public static CreateRecurringExpenseUseCaseOutput Failure(IReadOnlyCollection<FieldError> errors) =>
        new(
            isSuccess: false,
            id: null,
            name: null,
            category: null,
            monthlyAmount: null,
            dueDay: null,
            startDate: null,
            frequency: null,
            status: null,
            note: null,
            occurrences: null,
            errors: errors);
}
