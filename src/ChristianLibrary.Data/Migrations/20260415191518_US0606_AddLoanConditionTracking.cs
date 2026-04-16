using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChristianLibrary.Data.Migrations
{
    /// <inheritdoc />
    public partial class US0606_AddLoanConditionTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReturnCondition",
                table: "Loans",
                newName: "ConditionAtReturn");

            migrationBuilder.AddColumn<int>(
                name: "ConditionAtCheckout",
                table: "Loans",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConditionAtCheckout",
                table: "Loans");

            migrationBuilder.RenameColumn(
                name: "ConditionAtReturn",
                table: "Loans",
                newName: "ReturnCondition");
        }
    }
}
