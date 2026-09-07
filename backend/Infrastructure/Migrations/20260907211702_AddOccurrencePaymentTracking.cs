using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContasEmDia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOccurrencePaymentTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "Occurrences",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PaymentDate",
                table: "Occurrences",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "Occurrences");

            migrationBuilder.DropColumn(
                name: "PaymentDate",
                table: "Occurrences");
        }
    }
}
