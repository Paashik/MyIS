using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeManufacturerCodeNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_manufacturers_ExternalSystem_ExternalId",
                schema: "integration",
                table: "manufacturers");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                schema: "integration",
                table: "manufacturers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_manufacturers_ExternalSystem_ExternalId",
                schema: "integration",
                table: "manufacturers",
                columns: new[] { "ExternalSystem", "ExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_manufacturers_ExternalSystem_ExternalId",
                schema: "integration",
                table: "manufacturers");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                schema: "integration",
                table: "manufacturers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_manufacturers_ExternalSystem_ExternalId",
                schema: "integration",
                table: "manufacturers",
                columns: new[] { "ExternalSystem", "ExternalId" },
                unique: true,
                filter: "[ExternalSystem] IS NOT NULL AND [ExternalId] IS NOT NULL");
        }
    }
}
