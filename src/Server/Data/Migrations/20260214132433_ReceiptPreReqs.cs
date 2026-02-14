using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MannaHp.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReceiptPreReqs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "order_number_seq",
                startValue: 1001L);

            migrationBuilder.AddColumn<string>(
                name: "CardBrand",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardLast4",
                table: "orders",
                type: "character varying(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderNumber",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValueSql: "nextval('order_number_seq')");

            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_settings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "app_settings",
                columns: new[] { "Id", "Key", "Value" },
                values: new object[,]
                {
                    { new Guid("e0000000-0001-0000-0000-000000000001"), "StoreName", "Manna + HP" },
                    { new Guid("e0000000-0002-0000-0000-000000000002"), "StoreAddress", "317 S Main St" },
                    { new Guid("e0000000-0003-0000-0000-000000000003"), "StoreCity", "Lindsay, OK 73052" },
                    { new Guid("e0000000-0004-0000-0000-000000000004"), "StorePhone", "(405) 208-2271" },
                    { new Guid("e0000000-0005-0000-0000-000000000005"), "DefaultTaxRate", "0.0825" },
                    { new Guid("e0000000-0006-0000-0000-000000000006"), "ReceiptFooter", "Our pleasure to serve you!" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_orders_OrderNumber",
                table: "orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_app_settings_Key",
                table: "app_settings",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_settings");

            migrationBuilder.DropIndex(
                name: "IX_orders_OrderNumber",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CardBrand",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CardLast4",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "OrderNumber",
                table: "orders");

            migrationBuilder.DropSequence(
                name: "order_number_seq");
        }
    }
}
