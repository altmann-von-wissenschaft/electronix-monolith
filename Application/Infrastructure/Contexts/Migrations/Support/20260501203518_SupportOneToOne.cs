using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Application.Infrastructure.Contexts.Migrations.Support
{
    /// <inheritdoc />
    public partial class SupportOneToOne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Answers_QuestionId",
                schema: "support",
                table: "Answers");

            migrationBuilder.CreateIndex(
                name: "IX_Answer_QuestionId",
                schema: "support",
                table: "Answers",
                column: "QuestionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Answer_QuestionId",
                schema: "support",
                table: "Answers");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_QuestionId",
                schema: "support",
                table: "Answers",
                column: "QuestionId");
        }
    }
}
