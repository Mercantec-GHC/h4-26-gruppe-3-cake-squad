using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wavelength.Migrations
{
    /// <inheritdoc />
    public partial class ProfilPictureAddIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PPictureAlt",
                table: "ProfilePictures",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PPictureAlt",
                table: "ProfilePictures");
        }
    }
}
