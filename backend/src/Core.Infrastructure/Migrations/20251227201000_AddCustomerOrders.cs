using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyIS.Core.Infrastructure.Data;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20251227201000_AddCustomerOrders")]
    public partial class AddCustomerOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "customers");

            migrationBuilder.CreateTable(
                name: "customer_orders",
                schema: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    order_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    delivery_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    state = table.Column<int>(type: "integer", nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    contract = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    store_id = table.Column<int>(type: "integer", nullable: true),
                    path = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    pay_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    finished_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    contact_id = table.Column<int>(type: "integer", nullable: true),
                    discount = table.Column<int>(type: "integer", nullable: true),
                    tax = table.Column<int>(type: "integer", nullable: true),
                    mark = table.Column<int>(type: "integer", nullable: true),
                    pn = table.Column<int>(type: "integer", nullable: true),
                    payment_form = table.Column<int>(type: "integer", nullable: true),
                    pay_method = table.Column<int>(type: "integer", nullable: true),
                    pay_period = table.Column<int>(type: "integer", nullable: true),
                    prepayment = table.Column<int>(type: "integer", nullable: true),
                    kind = table.Column<int>(type: "integer", nullable: true),
                    account_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_orders", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_orders_counterparties_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "mdm",
                        principalTable: "counterparties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_orders_employees_person_id",
                        column: x => x.person_id,
                        principalSchema: "org",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_orders_customer_id",
                schema: "customers",
                table: "customer_orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_orders_person_id",
                schema: "customers",
                table: "customer_orders",
                column: "person_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_orders",
                schema: "customers");
        }
    }
}
