using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MannaHp.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDrinkAddOnsSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "menu_item_available_ingredients",
                columns: new[] { "Id", "Active", "CustomerPrice", "GroupName", "IngredientId", "MenuItemId", "QuantityUsed", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("e0000000-0011-0000-0000-000000000000"), true, 1.00m, "Extras", new Guid("b0000000-000e-0000-0000-000000000014"), new Guid("c0000000-0009-0000-0000-000000000009"), 1.0m, 1 },
                    { new Guid("e0000000-0012-0000-0000-000000000000"), true, 0.50m, "Extras", new Guid("b0000000-0019-0000-0000-000000000025"), new Guid("c0000000-0009-0000-0000-000000000009"), 1.5m, 2 },
                    { new Guid("e0000000-0013-0000-0000-000000000000"), true, 0.75m, "Milk Substitute", new Guid("b0000000-0010-0000-0000-000000000016"), new Guid("c0000000-0009-0000-0000-000000000009"), 2.0m, 1 },
                    { new Guid("e0000000-0014-0000-0000-000000000000"), true, 1.00m, "Extras", new Guid("b0000000-000e-0000-0000-000000000014"), new Guid("c0000000-0008-0000-0000-000000000008"), 1.0m, 1 },
                    { new Guid("e0000000-0015-0000-0000-000000000000"), true, 0.50m, "Extras", new Guid("b0000000-0019-0000-0000-000000000025"), new Guid("c0000000-0008-0000-0000-000000000008"), 1.5m, 2 },
                    { new Guid("e0000000-0016-0000-0000-000000000000"), true, 0.75m, "Milk Substitute", new Guid("b0000000-0010-0000-0000-000000000016"), new Guid("c0000000-0008-0000-0000-000000000008"), 2.0m, 1 },
                    { new Guid("e0000000-0017-0000-0000-000000000000"), true, 1.00m, "Extras", new Guid("b0000000-000e-0000-0000-000000000014"), new Guid("c0000000-0005-0000-0000-000000000005"), 1.0m, 1 },
                    { new Guid("e0000000-0018-0000-0000-000000000000"), true, 0.50m, "Extras", new Guid("b0000000-0019-0000-0000-000000000025"), new Guid("c0000000-0005-0000-0000-000000000005"), 1.5m, 2 },
                    { new Guid("e0000000-0019-0000-0000-000000000000"), true, 1.00m, "Extras", new Guid("b0000000-000e-0000-0000-000000000014"), new Guid("c0000000-000a-0000-0000-000000000010"), 1.0m, 1 },
                    { new Guid("e0000000-0020-0000-0000-000000000000"), true, 0.50m, "Extras", new Guid("b0000000-0019-0000-0000-000000000025"), new Guid("c0000000-000a-0000-0000-000000000010"), 1.5m, 2 },
                    { new Guid("e0000000-0021-0000-0000-000000000000"), true, 0.75m, "Milk Substitute", new Guid("b0000000-0010-0000-0000-000000000016"), new Guid("c0000000-000a-0000-0000-000000000010"), 2.0m, 1 },
                    { new Guid("e0000000-0022-0000-0000-000000000000"), true, 1.00m, "Extras", new Guid("b0000000-000e-0000-0000-000000000014"), new Guid("c0000000-000b-0000-0000-000000000011"), 1.0m, 1 },
                    { new Guid("e0000000-0023-0000-0000-000000000000"), true, 0.50m, "Extras", new Guid("b0000000-0019-0000-0000-000000000025"), new Guid("c0000000-000b-0000-0000-000000000011"), 1.5m, 2 },
                    { new Guid("e0000000-0024-0000-0000-000000000000"), true, 0.75m, "Milk Substitute", new Guid("b0000000-0010-0000-0000-000000000016"), new Guid("c0000000-000b-0000-0000-000000000011"), 2.0m, 1 },
                    { new Guid("e0000000-0025-0000-0000-000000000000"), true, 1.00m, "Extras", new Guid("b0000000-000e-0000-0000-000000000014"), new Guid("c0000000-0006-0000-0000-000000000006"), 1.0m, 1 },
                    { new Guid("e0000000-0026-0000-0000-000000000000"), true, 0.50m, "Extras", new Guid("b0000000-0019-0000-0000-000000000025"), new Guid("c0000000-0006-0000-0000-000000000006"), 1.5m, 2 },
                    { new Guid("e0000000-0027-0000-0000-000000000000"), true, 0.75m, "Milk Substitute", new Guid("b0000000-0010-0000-0000-000000000016"), new Guid("c0000000-0006-0000-0000-000000000006"), 2.0m, 1 },
                    { new Guid("e0000000-0028-0000-0000-000000000000"), true, 1.00m, "Extras", new Guid("b0000000-000e-0000-0000-000000000014"), new Guid("c0000000-000c-0000-0000-000000000012"), 1.0m, 1 },
                    { new Guid("e0000000-0029-0000-0000-000000000000"), true, 0.50m, "Extras", new Guid("b0000000-0019-0000-0000-000000000025"), new Guid("c0000000-000c-0000-0000-000000000012"), 1.5m, 2 },
                    { new Guid("e0000000-0030-0000-0000-000000000000"), true, 0.75m, "Milk Substitute", new Guid("b0000000-0010-0000-0000-000000000016"), new Guid("c0000000-000c-0000-0000-000000000012"), 2.0m, 1 },
                    { new Guid("e0000000-0031-0000-0000-000000000000"), true, 1.00m, "Extras", new Guid("b0000000-000e-0000-0000-000000000014"), new Guid("c0000000-000d-0000-0000-000000000013"), 1.0m, 1 },
                    { new Guid("e0000000-0032-0000-0000-000000000000"), true, 0.50m, "Extras", new Guid("b0000000-0019-0000-0000-000000000025"), new Guid("c0000000-000d-0000-0000-000000000013"), 1.5m, 2 },
                    { new Guid("e0000000-0033-0000-0000-000000000000"), true, 0.75m, "Milk Substitute", new Guid("b0000000-0010-0000-0000-000000000016"), new Guid("c0000000-000d-0000-0000-000000000013"), 2.0m, 1 },
                    { new Guid("e0000000-0034-0000-0000-000000000000"), true, 1.00m, "Extras", new Guid("b0000000-000e-0000-0000-000000000014"), new Guid("c0000000-0007-0000-0000-000000000007"), 1.0m, 1 },
                    { new Guid("e0000000-0035-0000-0000-000000000000"), true, 0.75m, "Milk Substitute", new Guid("b0000000-0010-0000-0000-000000000016"), new Guid("c0000000-0007-0000-0000-000000000007"), 2.0m, 1 },
                    { new Guid("e0000000-0036-0000-0000-000000000000"), true, 1.00m, "Extras", new Guid("b0000000-000e-0000-0000-000000000014"), new Guid("c0000000-0003-0000-0000-000000000003"), 1.0m, 1 },
                    { new Guid("e0000000-0037-0000-0000-000000000000"), true, 0.75m, "Milk Substitute", new Guid("b0000000-0010-0000-0000-000000000016"), new Guid("c0000000-0003-0000-0000-000000000003"), 2.0m, 1 },
                    { new Guid("e0000000-0038-0000-0000-000000000000"), true, 1.00m, "Extras", new Guid("b0000000-000e-0000-0000-000000000014"), new Guid("c0000000-0010-0000-0000-000000000016"), 1.0m, 1 },
                    { new Guid("e0000000-0039-0000-0000-000000000000"), true, 0.50m, "Extras", new Guid("b0000000-0019-0000-0000-000000000025"), new Guid("c0000000-0010-0000-0000-000000000016"), 1.5m, 2 },
                    { new Guid("e0000000-0040-0000-0000-000000000000"), true, 0.75m, "Milk Substitute", new Guid("b0000000-0010-0000-0000-000000000016"), new Guid("c0000000-0010-0000-0000-000000000016"), 2.0m, 1 },
                    { new Guid("e0000000-0041-0000-0000-000000000000"), true, 1.00m, "Extras", new Guid("b0000000-000e-0000-0000-000000000014"), new Guid("c0000000-000f-0000-0000-000000000015"), 1.0m, 1 },
                    { new Guid("e0000000-0042-0000-0000-000000000000"), true, 0.50m, "Extras", new Guid("b0000000-0019-0000-0000-000000000025"), new Guid("c0000000-000f-0000-0000-000000000015"), 1.5m, 2 },
                    { new Guid("e0000000-0043-0000-0000-000000000000"), true, 0.75m, "Milk Substitute", new Guid("b0000000-0010-0000-0000-000000000016"), new Guid("c0000000-000f-0000-0000-000000000015"), 2.0m, 1 },
                    { new Guid("e0000000-0044-0000-0000-000000000000"), true, 1.00m, "Extras", new Guid("b0000000-000e-0000-0000-000000000014"), new Guid("c0000000-000e-0000-0000-000000000014"), 1.0m, 1 },
                    { new Guid("e0000000-0045-0000-0000-000000000000"), true, 0.50m, "Extras", new Guid("b0000000-0019-0000-0000-000000000025"), new Guid("c0000000-000e-0000-0000-000000000014"), 1.5m, 2 },
                    { new Guid("e0000000-0046-0000-0000-000000000000"), true, 1.00m, "Extras", new Guid("b0000000-000e-0000-0000-000000000014"), new Guid("c0000000-0012-0000-0000-000000000018"), 1.0m, 1 },
                    { new Guid("e0000000-0047-0000-0000-000000000000"), true, 0.50m, "Extras", new Guid("b0000000-0019-0000-0000-000000000025"), new Guid("c0000000-0012-0000-0000-000000000018"), 1.5m, 2 },
                    { new Guid("e0000000-0048-0000-0000-000000000000"), true, 1.00m, "Extras", new Guid("b0000000-000e-0000-0000-000000000014"), new Guid("c0000000-0013-0000-0000-000000000019"), 1.0m, 1 },
                    { new Guid("e0000000-0049-0000-0000-000000000000"), true, 0.50m, "Extras", new Guid("b0000000-0019-0000-0000-000000000025"), new Guid("c0000000-0013-0000-0000-000000000019"), 1.5m, 2 },
                    { new Guid("e0000000-0050-0000-0000-000000000000"), true, 1.00m, "Extras", new Guid("b0000000-000e-0000-0000-000000000014"), new Guid("c0000000-0018-0000-0000-000000000024"), 1.0m, 1 },
                    { new Guid("e0000000-0051-0000-0000-000000000000"), true, 0.50m, "Extras", new Guid("b0000000-0019-0000-0000-000000000025"), new Guid("c0000000-0018-0000-0000-000000000024"), 1.5m, 2 },
                    { new Guid("e0000000-0052-0000-0000-000000000000"), true, 0.75m, "Milk Substitute", new Guid("b0000000-0010-0000-0000-000000000016"), new Guid("c0000000-0018-0000-0000-000000000024"), 2.0m, 1 },
                    { new Guid("e0000000-0053-0000-0000-000000000000"), true, 1.00m, "Extras", new Guid("b0000000-000e-0000-0000-000000000014"), new Guid("c0000000-0015-0000-0000-000000000021"), 1.0m, 1 },
                    { new Guid("e0000000-0054-0000-0000-000000000000"), true, 0.50m, "Extras", new Guid("b0000000-0019-0000-0000-000000000025"), new Guid("c0000000-0015-0000-0000-000000000021"), 1.5m, 2 },
                    { new Guid("e0000000-0055-0000-0000-000000000000"), true, 0.75m, "Milk Substitute", new Guid("b0000000-0010-0000-0000-000000000016"), new Guid("c0000000-0015-0000-0000-000000000021"), 2.0m, 1 },
                    { new Guid("e0000000-0056-0000-0000-000000000000"), true, 0.50m, "Extras", new Guid("b0000000-0019-0000-0000-000000000025"), new Guid("c0000000-0016-0000-0000-000000000022"), 1.5m, 1 },
                    { new Guid("e0000000-0057-0000-0000-000000000000"), true, 0.75m, "Milk Substitute", new Guid("b0000000-0010-0000-0000-000000000016"), new Guid("c0000000-0016-0000-0000-000000000022"), 2.0m, 1 },
                    { new Guid("e0000000-0058-0000-0000-000000000000"), true, 1.00m, "Extras", new Guid("b0000000-000e-0000-0000-000000000014"), new Guid("c0000000-0002-0000-0000-000000000002"), 1.0m, 1 },
                    { new Guid("e0000000-0059-0000-0000-000000000000"), true, 0.50m, "Extras", new Guid("b0000000-0019-0000-0000-000000000025"), new Guid("c0000000-0002-0000-0000-000000000002"), 1.5m, 2 },
                    { new Guid("e0000000-0060-0000-0000-000000000000"), true, 1.00m, "Extras", new Guid("b0000000-000e-0000-0000-000000000014"), new Guid("c0000000-0019-0000-0000-000000000025"), 1.0m, 1 },
                    { new Guid("e0000000-0061-0000-0000-000000000000"), true, 0.50m, "Extras", new Guid("b0000000-0019-0000-0000-000000000025"), new Guid("c0000000-0019-0000-0000-000000000025"), 1.5m, 2 },
                    { new Guid("e0000000-0062-0000-0000-000000000000"), true, 0.75m, "Milk Substitute", new Guid("b0000000-0010-0000-0000-000000000016"), new Guid("c0000000-0019-0000-0000-000000000025"), 2.0m, 1 },
                    { new Guid("e0000000-0063-0000-0000-000000000000"), true, 1.00m, "Extras", new Guid("b0000000-000e-0000-0000-000000000014"), new Guid("c0000000-001a-0000-0000-000000000026"), 1.0m, 1 },
                    { new Guid("e0000000-0064-0000-0000-000000000000"), true, 0.50m, "Extras", new Guid("b0000000-0019-0000-0000-000000000025"), new Guid("c0000000-001a-0000-0000-000000000026"), 1.5m, 2 },
                    { new Guid("e0000000-0065-0000-0000-000000000000"), true, 0.75m, "Milk Substitute", new Guid("b0000000-0010-0000-0000-000000000016"), new Guid("c0000000-001a-0000-0000-000000000026"), 2.0m, 1 },
                    { new Guid("e0000000-0066-0000-0000-000000000000"), true, 1.00m, "Extras", new Guid("b0000000-000e-0000-0000-000000000014"), new Guid("c0000000-001b-0000-0000-000000000027"), 1.0m, 1 },
                    { new Guid("e0000000-0067-0000-0000-000000000000"), true, 0.50m, "Extras", new Guid("b0000000-0019-0000-0000-000000000025"), new Guid("c0000000-001b-0000-0000-000000000027"), 1.5m, 2 },
                    { new Guid("e0000000-0068-0000-0000-000000000000"), true, 0.75m, "Milk Substitute", new Guid("b0000000-0010-0000-0000-000000000016"), new Guid("c0000000-001b-0000-0000-000000000027"), 2.0m, 1 },
                    { new Guid("e0000000-0069-0000-0000-000000000000"), true, 1.00m, "Extras", new Guid("b0000000-000e-0000-0000-000000000014"), new Guid("c0000000-001c-0000-0000-000000000028"), 1.0m, 1 },
                    { new Guid("e0000000-0070-0000-0000-000000000000"), true, 0.50m, "Extras", new Guid("b0000000-0019-0000-0000-000000000025"), new Guid("c0000000-001c-0000-0000-000000000028"), 1.5m, 2 },
                    { new Guid("e0000000-0071-0000-0000-000000000000"), true, 0.75m, "Milk Substitute", new Guid("b0000000-0010-0000-0000-000000000016"), new Guid("c0000000-001c-0000-0000-000000000028"), 2.0m, 1 },
                    { new Guid("e0000000-0072-0000-0000-000000000000"), true, 1.00m, "Extras", new Guid("b0000000-000e-0000-0000-000000000014"), new Guid("c0000000-001d-0000-0000-000000000029"), 1.0m, 1 },
                    { new Guid("e0000000-0073-0000-0000-000000000000"), true, 0.50m, "Extras", new Guid("b0000000-0019-0000-0000-000000000025"), new Guid("c0000000-001d-0000-0000-000000000029"), 1.5m, 2 },
                    { new Guid("e0000000-0074-0000-0000-000000000000"), true, 0.75m, "Milk Substitute", new Guid("b0000000-0010-0000-0000-000000000016"), new Guid("c0000000-001d-0000-0000-000000000029"), 2.0m, 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0011-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0012-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0013-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0014-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0015-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0016-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0017-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0018-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0019-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0020-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0021-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0022-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0023-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0024-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0025-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0026-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0027-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0028-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0029-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0030-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0031-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0032-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0033-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0034-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0035-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0036-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0037-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0038-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0039-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0040-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0041-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0042-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0043-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0044-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0045-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0046-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0047-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0048-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0049-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0050-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0051-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0052-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0053-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0054-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0055-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0056-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0057-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0058-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0059-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0060-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0061-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0062-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0063-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0064-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0065-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0066-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0067-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0068-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0069-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0070-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0071-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0072-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0073-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                table: "menu_item_available_ingredients",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0074-0000-0000-000000000000"));
        }
    }
}
