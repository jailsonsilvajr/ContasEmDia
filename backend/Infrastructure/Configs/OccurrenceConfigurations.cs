using ContasEmDia.Domain.Entities;
using ContasEmDia.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContasEmDia.Infrastructure.Configs;

public sealed class OccurrenceConfigurations : IEntityTypeConfiguration<Occurrence>
{
    public void Configure(EntityTypeBuilder<Occurrence> builder)
    {
        builder.ToTable("Occurrences");

        builder.Property<Guid>("_id").HasColumnName("Id");
        builder.HasKey("_id");

        builder.Property<Guid>("RecurringExpenseId").IsRequired();

        builder.ComplexProperty<ReferencePeriod>("_referencePeriod", referencePeriod =>
        {
            referencePeriod.UsePropertyAccessMode(PropertyAccessMode.Field);
            referencePeriod.Property(p => p.Year).HasColumnName("ReferenceYear");
            referencePeriod.Property(p => p.Month).HasColumnName("ReferenceMonth");
        });

        builder.Property<CalendarDate>("_dueDate")
            .HasColumnName("DueDate")
            .HasConversion(vo => vo.GetValue(), value => new CalendarDate(value))
            .HasColumnType("date")
            .IsRequired();

        builder.Property<OccurrenceStatus>("_status")
            .HasColumnName("Status")
            .HasConversion(vo => (int)vo.GetValue(), value => new OccurrenceStatus((OccurrenceStatusType)value))
            .IsRequired();

        builder.Property<ExpenseName>("_name")
            .HasColumnName("Name")
            .HasConversion(vo => vo.GetValue(), value => new ExpenseName(value))
            .HasMaxLength(200)
            .IsRequired();

        builder.Property<ExpenseCategory>("_category")
            .HasColumnName("Category")
            .HasConversion(vo => (int)vo.GetValue(), value => new ExpenseCategory((ExpenseCategoryType)value))
            .IsRequired();

        builder.Property<Money>("_expectedAmount")
            .HasColumnName("ExpectedAmount")
            .HasConversion(vo => vo.GetValue(), value => new Money(value))
            .HasColumnType("decimal(18,2)")
            .IsRequired();
    }
}
