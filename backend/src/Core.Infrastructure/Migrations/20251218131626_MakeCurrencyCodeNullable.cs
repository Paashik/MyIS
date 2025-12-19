using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeCurrencyCodeNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_currencies_ExternalSystem_ExternalId",
                schema: "integration",
                table: "currencies");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                schema: "integration",
                table: "currencies",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.CreateIndex(
                name: "IX_currencies_ExternalSystem_ExternalId",
                schema: "integration",
                table: "currencies",
                columns: new[] { "ExternalSystem", "ExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_currencies_ExternalSystem_ExternalId",
                schema: "integration",
                table: "currencies");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                schema: "integration",
                table: "currencies",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_currencies_ExternalSystem_ExternalId",
                schema: "integration",
                table: "currencies",
                columns: new[] { "ExternalSystem", "ExternalId" },
                unique: true,
                filter: "[ExternalSystem] IS NOT NULL AND [ExternalId] IS NOT NULL");
        }
    }
}
