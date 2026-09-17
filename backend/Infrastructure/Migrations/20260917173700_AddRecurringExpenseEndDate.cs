using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContasEmDia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringExpenseEndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "RecurringExpenses",
                type: "date",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"RecurringExpenses\" SET \"EndDate\" = \"StartDate\" + INTERVAL '1 year' WHERE \"EndDate\" IS NULL;");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "EndDate",
                table: "RecurringExpenses",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "RecurringExpenses");
        }
    }
}
