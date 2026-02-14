using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MannaHp.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuItemImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "menu_items",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0001-0000-0000-000000000001"),
                column: "ImageUrl",
                value: "/menu/burrito-bowl.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0002-0000-0000-000000000002"),
                column: "ImageUrl",
                value: "/menu/drip-coffee.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0003-0000-0000-000000000003"),
                column: "ImageUrl",
                value: "/menu/cafe-au-lait.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0004-0000-0000-000000000004"),
                column: "ImageUrl",
                value: "/menu/espresso.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0005-0000-0000-000000000005"),
                column: "ImageUrl",
                value: "/menu/americano.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0006-0000-0000-000000000006"),
                column: "ImageUrl",
                value: "/menu/caramel-macchiato.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0007-0000-0000-000000000007"),
                column: "ImageUrl",
                value: "/menu/cortado.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0008-0000-0000-000000000008"),
                column: "ImageUrl",
                value: "/menu/cappuccino.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0009-0000-0000-000000000009"),
                column: "ImageUrl",
                value: "/menu/latte.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-000a-0000-0000-000000000010"),
                column: "ImageUrl",
                value: "/menu/mocha.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-000b-0000-0000-000000000011"),
                column: "ImageUrl",
                value: "/menu/white-mocha.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-000c-0000-0000-000000000012"),
                column: "ImageUrl",
                value: "/menu/flavored-latte.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-000d-0000-0000-000000000013"),
                column: "ImageUrl",
                value: "/menu/caramel-latte.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-000e-0000-0000-000000000014"),
                column: "ImageUrl",
                value: "/menu/iced-coffee.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-000f-0000-0000-000000000015"),
                column: "ImageUrl",
                value: "/menu/cold-brew.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0010-0000-0000-000000000016"),
                column: "ImageUrl",
                value: "/menu/iced-latte.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0011-0000-0000-000000000017"),
                column: "ImageUrl",
                value: "/menu/affogato.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0012-0000-0000-000000000018"),
                column: "ImageUrl",
                value: "/menu/blended-mocha.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0013-0000-0000-000000000019"),
                column: "ImageUrl",
                value: "/menu/blended-caramel.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0014-0000-0000-000000000020"),
                column: "ImageUrl",
                value: "/menu/smoothie.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0015-0000-0000-000000000021"),
                column: "ImageUrl",
                value: "/menu/hot-chocolate.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0016-0000-0000-000000000022"),
                column: "ImageUrl",
                value: "/menu/steamer.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0017-0000-0000-000000000023"),
                column: "ImageUrl",
                value: "/menu/tea.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0018-0000-0000-000000000024"),
                column: "ImageUrl",
                value: "/menu/chai-latte.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0019-0000-0000-000000000025"),
                column: "ImageUrl",
                value: "/menu/pumpkin-spice-latte.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-001a-0000-0000-000000000026"),
                column: "ImageUrl",
                value: "/menu/maple-brown-sugar-latte.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-001b-0000-0000-000000000027"),
                column: "ImageUrl",
                value: "/menu/toasted-marshmallow-mocha.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-001c-0000-0000-000000000028"),
                column: "ImageUrl",
                value: "/menu/peppermint-mocha.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-001d-0000-0000-000000000029"),
                column: "ImageUrl",
                value: "/menu/gingerbread-latte.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-001e-0000-0000-000000000030"),
                column: "ImageUrl",
                value: "/menu/apple-cider.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-001f-0000-0000-000000000031"),
                column: "ImageUrl",
                value: "/menu/chips.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0020-0000-0000-000000000032"),
                column: "ImageUrl",
                value: "/menu/chips-queso.jpg");

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0021-0000-0000-000000000033"),
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0022-0000-0000-000000000034"),
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0023-0000-0000-000000000035"),
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0024-0000-0000-000000000036"),
                column: "ImageUrl",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "menu_items");
        }
    }
}
