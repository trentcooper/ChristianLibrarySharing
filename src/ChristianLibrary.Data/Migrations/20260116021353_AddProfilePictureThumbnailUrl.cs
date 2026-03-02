using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChristianLibrary.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePictureThumbnailUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureThumbnailUrl",
                table: "UserProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePictureThumbnailUrl",
                table: "UserProfiles");
        }
    }
}
