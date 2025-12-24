using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMdmItemNomenclatureNoAndDesignation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Designation",
                schema: "mdm",
                table: "items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomenclatureNo",
                schema: "mdm",
                table: "items",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE mdm.items
                SET "NomenclatureNo" = "Code"
                WHERE "NomenclatureNo" IS NULL OR "NomenclatureNo" = '';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "NomenclatureNo",
                schema: "mdm",
                table: "items",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_items_NomenclatureNo",
                schema: "mdm",
                table: "items",
                column: "NomenclatureNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_items_NomenclatureNo",
                schema: "mdm",
                table: "items");

            migrationBuilder.DropColumn(
                name: "Designation",
                schema: "mdm",
                table: "items");

            migrationBuilder.DropColumn(
                name: "NomenclatureNo",
                schema: "mdm",
                table: "items");
        }
    }
}
