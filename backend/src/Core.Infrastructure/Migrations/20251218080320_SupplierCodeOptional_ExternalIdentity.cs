using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupplierCodeOptional_ExternalIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_suppliers_Code",
                schema: "mdm",
                table: "suppliers");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                schema: "mdm",
                table: "suppliers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_Code",
                schema: "mdm",
                table: "suppliers",
                column: "Code",
                unique: true,
                filter: "\"Code\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_suppliers_Code",
                schema: "mdm",
                table: "suppliers");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                schema: "mdm",
                table: "suppliers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_Code",
                schema: "mdm",
                table: "suppliers",
                column: "Code",
                unique: true);
        }
    }
}
