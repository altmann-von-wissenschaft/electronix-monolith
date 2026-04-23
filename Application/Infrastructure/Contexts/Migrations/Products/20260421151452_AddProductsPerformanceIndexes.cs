using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Application.Infrastructure.Contexts.Migrations.Products
{
    /// <inheritdoc />
    public partial class AddProductsPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_ParentId",
                schema: "products",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId_IsHidden",
                schema: "products",
                table: "Products",
                columns: new[] { "CategoryId", "IsHidden" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CreatedAt",
                schema: "products",
                table: "Products",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsHidden",
                schema: "products",
                table: "Products",
                column: "IsHidden");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentId_DisplayOrder_Name",
                schema: "products",
                table: "Categories",
                columns: new[] { "ParentId", "DisplayOrder", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_CategoryId_IsHidden",
                schema: "products",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_CreatedAt",
                schema: "products",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsHidden",
                schema: "products",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Categories_ParentId_DisplayOrder_Name",
                schema: "products",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentId",
                schema: "products",
                table: "Categories",
                column: "ParentId");
        }
    }
}
