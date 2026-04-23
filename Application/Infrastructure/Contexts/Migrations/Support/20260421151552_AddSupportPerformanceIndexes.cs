using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Application.Infrastructure.Contexts.Migrations.Support
{
    /// <inheritdoc />
    public partial class AddSupportPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Questions_CreatedAt",
                schema: "support",
                table: "Questions",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_IsAnswered",
                schema: "support",
                table: "Questions",
                column: "IsAnswered");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_UserId_IsAnswered",
                schema: "support",
                table: "Questions",
                columns: new[] { "UserId", "IsAnswered" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Questions_CreatedAt",
                schema: "support",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_IsAnswered",
                schema: "support",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_UserId_IsAnswered",
                schema: "support",
                table: "Questions");
        }
    }
}
