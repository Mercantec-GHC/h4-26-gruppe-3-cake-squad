using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wavelength.Migrations
{
    /// <inheritdoc />
    public partial class MovedUserVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsUserVisible",
                table: "QuestionScores");

            migrationBuilder.AlterColumn<string>(
                name: "Visibility",
                table: "UserVisibilities",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Visibility",
                table: "UserVisibilities",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<bool>(
                name: "IsUserVisible",
                table: "QuestionScores",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
