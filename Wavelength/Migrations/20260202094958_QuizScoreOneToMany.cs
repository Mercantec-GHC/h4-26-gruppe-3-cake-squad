using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wavelength.Migrations
{
    /// <inheritdoc />
    public partial class QuizScoreOneToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuestionScores_PlayerId",
                table: "QuestionScores");

            migrationBuilder.DropIndex(
                name: "IX_QuestionScores_QuizOwnerId",
                table: "QuestionScores");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionScores_PlayerId",
                table: "QuestionScores",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionScores_QuizOwnerId",
                table: "QuestionScores",
                column: "QuizOwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuestionScores_PlayerId",
                table: "QuestionScores");

            migrationBuilder.DropIndex(
                name: "IX_QuestionScores_QuizOwnerId",
                table: "QuestionScores");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionScores_PlayerId",
                table: "QuestionScores",
                column: "PlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionScores_QuizOwnerId",
                table: "QuestionScores",
                column: "QuizOwnerId",
                unique: true);
        }
    }
}
