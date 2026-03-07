using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MannaHp.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "store_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_tokens", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "app_settings",
                columns: new[] { "Id", "Key", "Value" },
                values: new object[,]
                {
                    { new Guid("e0000000-0007-0000-0000-000000000007"), "StoreTokenDurationDays", "7" },
                    { new Guid("e0000000-0008-0000-0000-000000000008"), "StoreTokenRequiredMessage", "Please scan the QR code at our counter to place an in-store order." }
                });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "role-customer",
                column: "ConcurrencyStamp",
                value: "515ad03a-7c76-4c57-a7cd-e690e041afb8");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "role-owner",
                column: "ConcurrencyStamp",
                value: "7be6f24a-b4a5-4645-8597-94ba579cb602");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "role-staff",
                column: "ConcurrencyStamp",
                value: "5e4d4c33-c674-4a57-9459-7212bcc3c22f");

            migrationBuilder.CreateIndex(
                name: "IX_store_tokens_Token",
                table: "store_tokens",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "store_tokens");

            migrationBuilder.DeleteData(
                table: "app_settings",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0007-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "app_settings",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0008-0000-0000-000000000008"));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "role-customer",
                column: "ConcurrencyStamp",
                value: "72ccaaee-c567-486e-9cf8-eb48c011afb9");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "role-owner",
                column: "ConcurrencyStamp",
                value: "7c3afb9b-a679-4994-ab16-075539cf43f6");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "role-staff",
                column: "ConcurrencyStamp",
                value: "cc866ca4-ddb2-4bc4-82db-ab7e594d9f59");
        }
    }
}
