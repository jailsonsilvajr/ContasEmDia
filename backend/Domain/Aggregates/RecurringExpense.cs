using ContasEmDia.Domain.Entities;
using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Domain.Aggregates;

public sealed class RecurringExpense
{
    private readonly Guid _id;
    private readonly ExpenseName _name;
    private readonly ExpenseCategory _category;
    private readonly Money _monthlyAmount;
    private readonly DueDay _dueDay;
    private readonly CalendarDate _startDate;
    private readonly Frequency _frequency;
    private readonly RecurringExpenseStatus _status;
    private readonly Note _note;
    private readonly List<Occurrence> _occurrences = [];

    public RecurringExpense(
        ExpenseName name,
        ExpenseCategory category,
        Money monthlyAmount,
        DueDay dueDay,
        CalendarDate startDate,
        Frequency frequency,
        RecurringExpenseStatus status,
        Note note,
        ReferencePeriod currentReferencePeriod)
    {
        _id = Guid.NewGuid();
        _name = name;
        _category = category;
        _monthlyAmount = monthlyAmount;
        _dueDay = dueDay;
        _startDate = startDate;
        _frequency = frequency;
        _status = status;
        _note = note;

        var startPeriod = ReferencePeriod.FromDate(startDate.GetValue());

        if (status.GetValue() == RecurringExpenseStatusType.Active && currentReferencePeriod >= startPeriod)
        {
            var daysInMonth = DateTime.DaysInMonth(currentReferencePeriod.Year, currentReferencePeriod.Month);
            var dueDayOfMonth = Math.Min(dueDay.GetValue(), daysInMonth);
            var dueDate = new CalendarDate(new DateOnly(currentReferencePeriod.Year, currentReferencePeriod.Month, dueDayOfMonth));

            _occurrences.Add(new Occurrence(currentReferencePeriod, dueDate, name, category, monthlyAmount));
        }
    }

    public Guid GetId() => _id;

    public ExpenseName GetName() => _name;

    public ExpenseCategory GetCategory() => _category;

    public Money GetMonthlyAmount() => _monthlyAmount;

    public DueDay GetDueDay() => _dueDay;

    public CalendarDate GetStartDate() => _startDate;

    public Frequency GetFrequency() => _frequency;

    public RecurringExpenseStatus GetStatus() => _status;

    public Note GetNote() => _note;

    public IReadOnlyCollection<Occurrence> GetOccurrences() => _occurrences;
}
