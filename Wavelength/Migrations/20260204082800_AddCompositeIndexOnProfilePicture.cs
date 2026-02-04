using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wavelength.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeIndexOnProfilePicture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ProfilePictures_UserId_PictureType",
                table: "ProfilePictures",
                columns: new[] { "UserId", "PictureType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProfilePictures_UserId_PictureType",
                table: "ProfilePictures");
        }
    }
}
