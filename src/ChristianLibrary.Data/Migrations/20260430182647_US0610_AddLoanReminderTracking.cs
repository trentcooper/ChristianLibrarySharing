using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChristianLibrary.Data.Migrations
{
    /// <inheritdoc />
    public partial class US0610_AddLoanReminderTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastReminderCategory",
                table: "Loans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastReminderOffsetDays",
                table: "Loans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReminderSentAt",
                table: "Loans",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReminderCategory",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "LastReminderOffsetDays",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "LastReminderSentAt",
                table: "Loans");
        }
    }
}
