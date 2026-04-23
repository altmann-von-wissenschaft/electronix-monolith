using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Application.Infrastructure.Contexts.Migrations.Orders
{
    /// <inheritdoc />
    public partial class AddOrdersPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderStatusHistories_OrderId",
                schema: "orders",
                table: "OrderStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_OrderId",
                schema: "orders",
                table: "OrderItems");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistory_ChangedAt",
                schema: "orders",
                table: "OrderStatusHistories",
                column: "ChangedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistory_OrderId_ChangedAt",
                schema: "orders",
                table: "OrderStatusHistories",
                columns: new[] { "OrderId", "ChangedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedAt",
                schema: "orders",
                table: "Orders",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId_CreatedAt",
                schema: "orders",
                table: "Orders",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId_ProductId",
                schema: "orders",
                table: "OrderItems",
                columns: new[] { "OrderId", "ProductId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderStatusHistory_ChangedAt",
                schema: "orders",
                table: "OrderStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_OrderStatusHistory_OrderId_ChangedAt",
                schema: "orders",
                table: "OrderStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CreatedAt",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_UserId_CreatedAt",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_OrderId_ProductId",
                schema: "orders",
                table: "OrderItems");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistories_OrderId",
                schema: "orders",
                table: "OrderStatusHistories",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                schema: "orders",
                table: "OrderItems",
                column: "OrderId");
        }
    }
}
