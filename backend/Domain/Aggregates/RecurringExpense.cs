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
    private CalendarDate _endDate;
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
        CalendarDate endDate,
        Frequency frequency,
        RecurringExpenseStatus status,
        Note note,
        ReferencePeriod currentReferencePeriod)
    {
        ValidateVigencia(startDate, endDate);
        ValidateEndDateNotInPast(endDate, currentReferencePeriod);

        _id = Guid.NewGuid();
        _name = name;
        _category = category;
        _monthlyAmount = monthlyAmount;
        _dueDay = dueDay;
        _startDate = startDate;
        _endDate = endDate;
        _frequency = frequency;
        _status = status;
        _note = note;

        GenerateOccurrencesForVigencia(currentReferencePeriod);
    }

    private RecurringExpense(
        Guid id,
        ExpenseName name,
        ExpenseCategory category,
        Money monthlyAmount,
        DueDay dueDay,
        CalendarDate startDate,
        CalendarDate endDate,
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
        _endDate = endDate;
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

    public CalendarDate GetEndDate() => _endDate;

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
        ValidateVigencia(newStartDate, _endDate);

        _startDate = newStartDate;
    }

    public void ChangeEndDate(CalendarDate newEndDate, ReferencePeriod currentReferencePeriod)
    {
        ValidateVigencia(_startDate, newEndDate);
        ValidateEndDateNotInPast(newEndDate, currentReferencePeriod);

        var newPeriod = ReferencePeriod.FromDate(newEndDate.GetValue());
        var oldPeriod = ReferencePeriod.FromDate(_endDate.GetValue());

        if (newPeriod > oldPeriod)
        {
            _endDate = newEndDate;

            for (var period = oldPeriod.Next(); period <= newPeriod; period = period.Next())
            {
                GenerateOccurrenceForPeriodIfMissing(period);
            }
        }
        else if (newPeriod < oldPeriod)
        {
            var hasPaidOccurrenceBeingRemoved = _occurrences.Any(occurrence =>
                occurrence.GetReferencePeriod() > newPeriod &&
                occurrence.GetStatus().GetValue() == OccurrenceStatusType.Paid);

            if (hasPaidOccurrenceBeingRemoved)
            {
                throw new DomainRuleViolationException(
                    "Não é possível reduzir a vigência: existe uma ocorrência já paga no período que seria removido.");
            }

            _endDate = newEndDate;
            _occurrences.RemoveAll(occurrence => occurrence.GetReferencePeriod() > newPeriod);
        }
        else
        {
            _endDate = newEndDate;
        }
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

        var startPeriod = ReferencePeriod.FromDate(_startDate.GetValue());
        if (currentReferencePeriod >= startPeriod)
        {
            GenerateOccurrenceForPeriodIfMissing(currentReferencePeriod);
        }
    }

    private static void ValidateVigencia(CalendarDate startDate, CalendarDate endDate)
    {
        if (endDate.GetValue() <= startDate.GetValue())
        {
            throw new DomainRuleViolationException("A data de fim deve ser posterior à data de início.");
        }

        if (endDate.GetValue() > startDate.GetValue().AddYears(1))
        {
            throw new DomainRuleViolationException("A vigência não pode ultrapassar 1 ano a partir da data de início.");
        }
    }

    private static void ValidateEndDateNotInPast(CalendarDate endDate, ReferencePeriod currentReferencePeriod)
    {
        if (ReferencePeriod.FromDate(endDate.GetValue()) < currentReferencePeriod)
        {
            throw new DomainRuleViolationException("A data de fim não pode estar no passado.");
        }
    }

    private void GenerateOccurrenceForPeriodIfMissing(ReferencePeriod period)
    {
        if (GetOccurrencesForPeriod(period).Count == 0)
        {
            var daysInMonth = DateTime.DaysInMonth(period.Year, period.Month);
            var dueDayOfMonth = Math.Min(_dueDay.GetValue(), daysInMonth);
            var dueDate = new CalendarDate(new DateOnly(period.Year, period.Month, dueDayOfMonth));

            _occurrences.Add(new Occurrence(period, dueDate, _name, _category, _monthlyAmount));
        }
    }

    private void GenerateOccurrencesForVigencia(ReferencePeriod currentReferencePeriod)
    {
        var startPeriod = ReferencePeriod.FromDate(_startDate.GetValue());
        if (currentReferencePeriod < startPeriod)
        {
            return;
        }

        var endPeriod = ReferencePeriod.FromDate(_endDate.GetValue());
        for (var period = currentReferencePeriod; period <= endPeriod; period = period.Next())
        {
            GenerateOccurrenceForPeriodIfMissing(period);
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
