using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Application.Infrastructure.Contexts.Migrations.Users
{
    /// <inheritdoc />
    public partial class AddPushNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FcmDeviceRegistrations",
                schema: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FcmDeviceRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FcmDeviceRegistrations_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "users",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPushPreferences",
                schema: "users",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotifyOrderStatus = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    NotifySupportReply = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    NotifyReviewModeration = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    NotifySupportQueue = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPushPreferences", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserPushPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "users",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FcmDeviceRegistrations_Token",
                schema: "users",
                table: "FcmDeviceRegistrations",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FcmDeviceRegistrations_UserId",
                schema: "users",
                table: "FcmDeviceRegistrations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FcmDeviceRegistrations",
                schema: "users");

            migrationBuilder.DropTable(
                name: "UserPushPreferences",
                schema: "users");
        }
    }
}
