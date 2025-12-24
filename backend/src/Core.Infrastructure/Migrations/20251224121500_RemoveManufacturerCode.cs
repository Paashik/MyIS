using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveManufacturerCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_manufacturers_Code",
                schema: "mdm",
                table: "manufacturers");

            migrationBuilder.DropColumn(
                name: "Code",
                schema: "mdm",
                table: "manufacturers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "mdm",
                table: "manufacturers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_manufacturers_Code",
                schema: "mdm",
                table: "manufacturers",
                column: "Code",
                unique: true);
        }
    }
}
