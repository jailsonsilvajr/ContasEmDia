using ContasEmDia.Domain.Aggregates;
using ContasEmDia.Domain.Entities;
using ContasEmDia.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContasEmDia.Infrastructure.Configs;

public sealed class RecurringExpenseConfigurations : IEntityTypeConfiguration<RecurringExpense>
{
    public void Configure(EntityTypeBuilder<RecurringExpense> builder)
    {
        builder.ToTable("RecurringExpenses");

        builder.Property<Guid>("_id").HasColumnName("Id");
        builder.HasKey("_id");

        builder.Property<ExpenseName>("_name")
            .HasColumnName("Name")
            .HasConversion(vo => vo.GetValue(), value => new ExpenseName(value))
            .HasMaxLength(200)
            .IsRequired();

        builder.Property<ExpenseCategory>("_category")
            .HasColumnName("Category")
            .HasConversion(vo => (int)vo.GetValue(), value => new ExpenseCategory((ExpenseCategoryType)value))
            .IsRequired();

        builder.Property<Money>("_monthlyAmount")
            .HasColumnName("MonthlyAmount")
            .HasConversion(vo => vo.GetValue(), value => new Money(value))
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property<DueDay>("_dueDay")
            .HasColumnName("DueDay")
            .HasConversion(vo => vo.GetValue(), value => new DueDay(value))
            .IsRequired();

        builder.Property<CalendarDate>("_startDate")
            .HasColumnName("StartDate")
            .HasConversion(vo => vo.GetValue(), value => new CalendarDate(value))
            .HasColumnType("date")
            .IsRequired();

        builder.Property<Frequency>("_frequency")
            .HasColumnName("Frequency")
            .HasConversion(vo => (int)vo.GetValue(), value => new Frequency((FrequencyType)value))
            .IsRequired();

        builder.Property<RecurringExpenseStatus>("_status")
            .HasColumnName("Status")
            .HasConversion(vo => (int)vo.GetValue(), value => new RecurringExpenseStatus((RecurringExpenseStatusType)value))
            .IsRequired();

        builder.Property<Note>("_note")
            .HasColumnName("Note")
            .HasConversion(vo => vo.GetValue(), value => new Note(value))
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.HasMany<Occurrence>("_occurrences")
            .WithOne()
            .HasForeignKey("RecurringExpenseId")
            .IsRequired();

        builder.Navigation("_occurrences").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
