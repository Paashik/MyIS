using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UomExternalKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                schema: "mdm",
                table: "unit_of_measures",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSystem",
                schema: "mdm",
                table: "unit_of_measures",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SyncedAt",
                schema: "mdm",
                table: "unit_of_measures",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_unit_of_measures_ExternalSystem_ExternalId",
                schema: "mdm",
                table: "unit_of_measures",
                columns: new[] { "ExternalSystem", "ExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_unit_of_measures_ExternalSystem_ExternalId",
                schema: "mdm",
                table: "unit_of_measures");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                schema: "mdm",
                table: "unit_of_measures");

            migrationBuilder.DropColumn(
                name: "ExternalSystem",
                schema: "mdm",
                table: "unit_of_measures");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                schema: "mdm",
                table: "unit_of_measures");
        }
    }
}
