using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCounterpartyCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_counterparties_Code",
                schema: "mdm",
                table: "counterparties");

            migrationBuilder.DropColumn(
                name: "Code",
                schema: "mdm",
                table: "counterparties");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "mdm",
                table: "counterparties",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_counterparties_Code",
                schema: "mdm",
                table: "counterparties",
                column: "Code",
                unique: true,
                filter: "\"Code\" IS NOT NULL");
        }
    }
}
