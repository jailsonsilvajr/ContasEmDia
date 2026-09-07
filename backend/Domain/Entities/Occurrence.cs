using ContasEmDia.Domain;
using ContasEmDia.Domain.ValueObjects;

namespace ContasEmDia.Domain.Entities;

public sealed class Occurrence
{
    private readonly Guid _id;
    private readonly ReferencePeriod _referencePeriod = new(1, 1);
    private readonly CalendarDate _dueDate;
    private OccurrenceStatus _status;
    private readonly ExpenseName _name;
    private readonly ExpenseCategory _category;
    private readonly Money _expectedAmount;
    private Money? _paidAmount;
    private CalendarDate? _paymentDate;

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

    private Occurrence(
        Guid id,
        CalendarDate dueDate,
        OccurrenceStatus status,
        ExpenseName name,
        ExpenseCategory category,
        Money expectedAmount,
        Money? paidAmount,
        CalendarDate? paymentDate)
    {
        _id = id;
        _dueDate = dueDate;
        _status = status;
        _name = name;
        _category = category;
        _expectedAmount = expectedAmount;
        _paidAmount = paidAmount;
        _paymentDate = paymentDate;
    }

    public Guid GetId() => _id;

    public ReferencePeriod GetReferencePeriod() => _referencePeriod;

    public CalendarDate GetDueDate() => _dueDate;

    public OccurrenceStatus GetStatus() => _status;

    public ExpenseName GetName() => _name;

    public ExpenseCategory GetCategory() => _category;

    public Money GetExpectedAmount() => _expectedAmount;

    public Money? GetPaidAmount() => _paidAmount;

    public CalendarDate? GetPaymentDate() => _paymentDate;

    public void MarkAsPaid(Money paidAmount, CalendarDate paymentDate)
    {
        if (_status.GetValue() == OccurrenceStatusType.Paid)
        {
            throw new DomainRuleViolationException("Esta ocorrência já está paga.");
        }

        _status = new OccurrenceStatus(OccurrenceStatusType.Paid);
        _paidAmount = paidAmount;
        _paymentDate = paymentDate;
    }

    public void UndoPayment()
    {
        if (_status.GetValue() == OccurrenceStatusType.Pending)
        {
            throw new DomainRuleViolationException("Esta ocorrência ainda não foi paga.");
        }

        _status = new OccurrenceStatus(OccurrenceStatusType.Pending);
        _paidAmount = null;
        _paymentDate = null;
    }

    public OccurrenceDerivedStatus GetDerivedStatus(DateOnly referenceDate)
    {
        if (_status.GetValue() == OccurrenceStatusType.Paid)
        {
            return new OccurrenceDerivedStatus(OccurrenceDerivedStatusType.Paid);
        }

        if (_dueDate.GetValue() < referenceDate)
        {
            return new OccurrenceDerivedStatus(OccurrenceDerivedStatusType.Overdue);
        }

        if (_dueDate.GetValue().DayNumber - referenceDate.DayNumber <= 7)
        {
            return new OccurrenceDerivedStatus(OccurrenceDerivedStatusType.DueSoon);
        }

        return new OccurrenceDerivedStatus(OccurrenceDerivedStatusType.Pending);
    }
}
