using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChristianLibrary.Data.Migrations
{
    /// <inheritdoc />
    public partial class US06010_AddLenderReminderCopyPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnLoanReminderCopies",
                table: "UserProfiles",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifyOnLoanReminderCopies",
                table: "UserProfiles");
        }
    }
}
