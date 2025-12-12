using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S1_Settings_RequestsDictionaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "requests",
                table: "request_types",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_enabled",
                schema: "requests",
                table: "request_transitions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "requests",
                table: "request_statuses",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "requests",
                table: "request_types");

            migrationBuilder.DropColumn(
                name: "is_enabled",
                schema: "requests",
                table: "request_transitions");

            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "requests",
                table: "request_statuses");
        }
    }
}
