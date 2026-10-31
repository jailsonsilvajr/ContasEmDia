using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContasEmDia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringExpenseDeletedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DeletedAt",
                table: "RecurringExpenses",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RecurringExpenses");
        }
    }
}
