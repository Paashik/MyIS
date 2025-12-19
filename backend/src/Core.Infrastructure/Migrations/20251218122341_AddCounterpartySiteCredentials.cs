using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCounterpartySiteCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SiteLogin",
                schema: "mdm",
                table: "counterparties",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SitePassword",
                schema: "mdm",
                table: "counterparties",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SiteLogin",
                schema: "mdm",
                table: "counterparties");

            migrationBuilder.DropColumn(
                name: "SitePassword",
                schema: "mdm",
                table: "counterparties");
        }
    }
}
