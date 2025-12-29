using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyIS.Core.Infrastructure.Data;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20251230120000_AddRequestBasisFields")]
    public partial class AddRequestBasisFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "basis_customer_order_id",
                schema: "requests",
                table: "requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "basis_description",
                schema: "requests",
                table: "requests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "basis_request_id",
                schema: "requests",
                table: "requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "basis_type",
                schema: "requests",
                table: "requests",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "basis_customer_order_id",
                schema: "requests",
                table: "requests");

            migrationBuilder.DropColumn(
                name: "basis_description",
                schema: "requests",
                table: "requests");

            migrationBuilder.DropColumn(
                name: "basis_request_id",
                schema: "requests",
                table: "requests");

            migrationBuilder.DropColumn(
                name: "basis_type",
                schema: "requests",
                table: "requests");
        }
    }
}
