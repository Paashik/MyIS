using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierContactsAndExternalRef : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                schema: "mdm",
                table: "suppliers",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "mdm",
                table: "suppliers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "mdm",
                table: "suppliers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                schema: "mdm",
                table: "suppliers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSystem",
                schema: "mdm",
                table: "suppliers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "mdm",
                table: "suppliers",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                schema: "mdm",
                table: "suppliers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProviderType",
                schema: "mdm",
                table: "suppliers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Site",
                schema: "mdm",
                table: "suppliers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SyncedAt",
                schema: "mdm",
                table: "suppliers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_ExternalSystem_ExternalId",
                schema: "mdm",
                table: "suppliers",
                columns: new[] { "ExternalSystem", "ExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_suppliers_ExternalSystem_ExternalId",
                schema: "mdm",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "Address",
                schema: "mdm",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "City",
                schema: "mdm",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "Email",
                schema: "mdm",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                schema: "mdm",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "ExternalSystem",
                schema: "mdm",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "mdm",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "Phone",
                schema: "mdm",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "ProviderType",
                schema: "mdm",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "Site",
                schema: "mdm",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                schema: "mdm",
                table: "suppliers");
        }
    }
}
