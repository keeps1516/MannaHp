using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MannaHp.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundsAndRestockPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RestockPolicy",
                table: "menu_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "refunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StripeRefundId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refunds_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refund_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RefundId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Restocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refund_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refund_items_order_items_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "order_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_refund_items_refunds_RefundId",
                        column: x => x.RefundId,
                        principalTable: "refunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0001-0000-0000-000000000001"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0002-0000-0000-000000000002"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0003-0000-0000-000000000003"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0004-0000-0000-000000000004"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0005-0000-0000-000000000005"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0006-0000-0000-000000000006"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0007-0000-0000-000000000007"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0008-0000-0000-000000000008"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0009-0000-0000-000000000009"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-000a-0000-0000-000000000010"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-000b-0000-0000-000000000011"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-000c-0000-0000-000000000012"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-000d-0000-0000-000000000013"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-000e-0000-0000-000000000014"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-000f-0000-0000-000000000015"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0010-0000-0000-000000000016"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0011-0000-0000-000000000017"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0012-0000-0000-000000000018"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0013-0000-0000-000000000019"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0014-0000-0000-000000000020"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0015-0000-0000-000000000021"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0016-0000-0000-000000000022"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0017-0000-0000-000000000023"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0018-0000-0000-000000000024"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0019-0000-0000-000000000025"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-001a-0000-0000-000000000026"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-001b-0000-0000-000000000027"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-001c-0000-0000-000000000028"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-001d-0000-0000-000000000029"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-001e-0000-0000-000000000030"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-001f-0000-0000-000000000031"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0020-0000-0000-000000000032"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0021-0000-0000-000000000033"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0022-0000-0000-000000000034"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0023-0000-0000-000000000035"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0024-0000-0000-000000000036"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "role-customer",
                column: "ConcurrencyStamp",
                value: "faa49170-793e-429a-8c1c-0d4ece95183e");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "role-owner",
                column: "ConcurrencyStamp",
                value: "5e050ebf-b946-439a-9493-8fa3028131b4");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: "role-staff",
                column: "ConcurrencyStamp",
                value: "9068d271-c953-4e19-9581-c4dab686fdaa");

            migrationBuilder.CreateIndex(
                name: "IX_refund_items_OrderItemId",
                table: "refund_items",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_refund_items_RefundId",
                table: "refund_items",
                column: "RefundId");

            migrationBuilder.CreateIndex(
                name: "IX_refunds_OrderId",
                table: "refunds",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_refunds_StripeRefundId",
                table: "refunds",
                column: "StripeRefundId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refund_items");

            migrationBuilder.DropTable(
                name: "refunds");

            migrationBuilder.DropColumn(
                name: "RestockPolicy",
                table: "menu_items");

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
        }
    }
}
