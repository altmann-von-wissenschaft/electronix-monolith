using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Application.Infrastructure.Contexts.Migrations.Cart
{
    /// <inheritdoc />
    public partial class AddCartPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId",
                schema: "cart",
                table: "CartItems");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_ProductId",
                schema: "cart",
                table: "CartItems",
                columns: new[] { "CartId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductId",
                schema: "cart",
                table: "CartItems",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId_ProductId",
                schema: "cart",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_ProductId",
                schema: "cart",
                table: "CartItems");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId",
                schema: "cart",
                table: "CartItems",
                column: "CartId");
        }
    }
}
