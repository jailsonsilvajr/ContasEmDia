using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Domain.Entities;

public sealed class Occurrence
{
    private readonly Guid _id;
    private readonly ReferencePeriod _referencePeriod;
    private readonly CalendarDate _dueDate;
    private readonly OccurrenceStatus _status;
    private readonly ExpenseName _name;
    private readonly ExpenseCategory _category;
    private readonly Money _expectedAmount;

    internal Occurrence(
        ReferencePeriod referencePeriod,
        CalendarDate dueDate,
        ExpenseName name,
        ExpenseCategory category,
        Money expectedAmount)
    {
        _id = Guid.NewGuid();
        _referencePeriod = referencePeriod;
        _dueDate = dueDate;
        _status = new OccurrenceStatus(OccurrenceStatusType.Pending);
        _name = name;
        _category = category;
        _expectedAmount = expectedAmount;
    }

    public Guid GetId() => _id;

    public ReferencePeriod GetReferencePeriod() => _referencePeriod;

    public CalendarDate GetDueDate() => _dueDate;

    public OccurrenceStatus GetStatus() => _status;

    public ExpenseName GetName() => _name;

    public ExpenseCategory GetCategory() => _category;

    public Money GetExpectedAmount() => _expectedAmount;
}
