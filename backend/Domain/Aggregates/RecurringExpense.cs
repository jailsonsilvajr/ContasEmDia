using ContasEmDia.Domain.Entities;
using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Domain.Aggregates;

public sealed class RecurringExpense
{
    private readonly Guid _id;
    private ExpenseName _name;
    private ExpenseCategory _category;
    private Money _monthlyAmount;
    private DueDay _dueDay;
    private CalendarDate _startDate;
    private readonly Frequency _frequency;
    private RecurringExpenseStatus _status;
    private Note _note;
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

        if (status.GetValue() == RecurringExpenseStatusType.Active)
        {
            GenerateOccurrenceForCurrentPeriodIfDue(currentReferencePeriod);
        }
    }

    private RecurringExpense(
        Guid id,
        ExpenseName name,
        ExpenseCategory category,
        Money monthlyAmount,
        DueDay dueDay,
        CalendarDate startDate,
        Frequency frequency,
        RecurringExpenseStatus status,
        Note note)
    {
        _id = id;
        _name = name;
        _category = category;
        _monthlyAmount = monthlyAmount;
        _dueDay = dueDay;
        _startDate = startDate;
        _frequency = frequency;
        _status = status;
        _note = note ?? new Note(null);
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

    public IReadOnlyCollection<Occurrence> GetOccurrencesForPeriod(ReferencePeriod referencePeriod) =>
        _occurrences
            .Where(occurrence =>
                occurrence.GetReferencePeriod().Year == referencePeriod.Year &&
                occurrence.GetReferencePeriod().Month == referencePeriod.Month)
            .ToList();

    public Occurrence? FindOccurrence(Guid occurrenceId) =>
        _occurrences.FirstOrDefault(occurrence => occurrence.GetId() == occurrenceId);

    public void Rename(ExpenseName newName)
    {
        _name = newName;
    }

    public void ChangeCategory(ExpenseCategory newCategory)
    {
        _category = newCategory;
    }

    public void ChangeMonthlyAmount(Money newMonthlyAmount)
    {
        _monthlyAmount = newMonthlyAmount;
    }

    public void ChangeDueDay(DueDay newDueDay)
    {
        _dueDay = newDueDay;
    }

    public void ChangeStartDate(CalendarDate newStartDate)
    {
        _startDate = newStartDate;
    }

    public void ChangeNote(Note newNote)
    {
        _note = newNote;
    }

    public void Pause()
    {
        _status = new RecurringExpenseStatus(RecurringExpenseStatusType.Paused);
    }

    public void Reactivate(ReferencePeriod currentReferencePeriod)
    {
        _status = new RecurringExpenseStatus(RecurringExpenseStatusType.Active);
        GenerateOccurrenceForCurrentPeriodIfDue(currentReferencePeriod);
    }

    private void GenerateOccurrenceForCurrentPeriodIfDue(ReferencePeriod currentReferencePeriod)
    {
        var startPeriod = ReferencePeriod.FromDate(_startDate.GetValue());

        if (currentReferencePeriod >= startPeriod && GetOccurrencesForPeriod(currentReferencePeriod).Count == 0)
        {
            var daysInMonth = DateTime.DaysInMonth(currentReferencePeriod.Year, currentReferencePeriod.Month);
            var dueDayOfMonth = Math.Min(_dueDay.GetValue(), daysInMonth);
            var dueDate = new CalendarDate(new DateOnly(currentReferencePeriod.Year, currentReferencePeriod.Month, dueDayOfMonth));

            _occurrences.Add(new Occurrence(currentReferencePeriod, dueDate, _name, _category, _monthlyAmount));
        }
    }

    public void MarkOccurrenceAsPaid(Guid occurrenceId, Money paidAmount, CalendarDate paymentDate)
    {
        var occurrence = FindOccurrence(occurrenceId) ?? throw new KeyNotFoundException("Ocorrência não encontrada.");

        occurrence.MarkAsPaid(paidAmount, paymentDate);
    }

    public void UndoOccurrencePayment(Guid occurrenceId)
    {
        var occurrence = FindOccurrence(occurrenceId) ?? throw new KeyNotFoundException("Ocorrência não encontrada.");

        occurrence.UndoPayment();
    }
}
