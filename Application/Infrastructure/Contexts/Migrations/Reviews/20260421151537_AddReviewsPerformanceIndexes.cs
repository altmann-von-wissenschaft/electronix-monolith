using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Application.Infrastructure.Contexts.Migrations.Reviews
{
    /// <inheritdoc />
    public partial class AddReviewsPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CreatedAt",
                schema: "reviews",
                table: "Reviews",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_IsApproved",
                schema: "reviews",
                table: "Reviews",
                column: "IsApproved");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ProductId_IsApproved",
                schema: "reviews",
                table: "Reviews",
                columns: new[] { "ProductId", "IsApproved" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId_IsApproved_CreatedAt",
                schema: "reviews",
                table: "Reviews",
                columns: new[] { "UserId", "IsApproved", "CreatedAt" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_CreatedAt",
                schema: "reviews",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_IsApproved",
                schema: "reviews",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ProductId_IsApproved",
                schema: "reviews",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_UserId_IsApproved_CreatedAt",
                schema: "reviews",
                table: "Reviews");
        }
    }
}
