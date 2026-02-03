using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wavelength.Migrations
{
    /// <inheritdoc />
    public partial class PictureBase64ToBytes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PPictureBase64",
                table: "ProfilePictures",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "PPictureAlt",
                table: "ProfilePictures",
                newName: "Name");

            migrationBuilder.AddColumn<byte[]>(
                name: "Data",
                table: "ProfilePictures",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Data",
                table: "ProfilePictures");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "ProfilePictures",
                newName: "PPictureBase64");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "ProfilePictures",
                newName: "PPictureAlt");
        }
    }
}
