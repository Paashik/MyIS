using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveMdmTablesToMdmSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "technical_parameters",
                schema: "integration",
                newName: "technical_parameters",
                newSchema: "mdm");

            migrationBuilder.RenameTable(
                name: "symbols",
                schema: "integration",
                newName: "symbols",
                newSchema: "mdm");

            migrationBuilder.RenameTable(
                name: "parameter_sets",
                schema: "integration",
                newName: "parameter_sets",
                newSchema: "mdm");

            migrationBuilder.RenameTable(
                name: "manufacturers",
                schema: "integration",
                newName: "manufacturers",
                newSchema: "mdm");

            migrationBuilder.RenameTable(
                name: "currencies",
                schema: "integration",
                newName: "currencies",
                newSchema: "mdm");

            migrationBuilder.RenameTable(
                name: "body_types",
                schema: "integration",
                newName: "body_types",
                newSchema: "mdm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "technical_parameters",
                schema: "mdm",
                newName: "technical_parameters",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "symbols",
                schema: "mdm",
                newName: "symbols",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "parameter_sets",
                schema: "mdm",
                newName: "parameter_sets",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "manufacturers",
                schema: "mdm",
                newName: "manufacturers",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "currencies",
                schema: "mdm",
                newName: "currencies",
                newSchema: "integration");

            migrationBuilder.RenameTable(
                name: "body_types",
                schema: "mdm",
                newName: "body_types",
                newSchema: "integration");
        }
    }
}
