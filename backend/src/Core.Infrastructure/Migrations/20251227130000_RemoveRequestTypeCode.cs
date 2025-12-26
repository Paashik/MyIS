using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRequestTypeCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_request_types_code",
                schema: "requests",
                table: "request_types");

            migrationBuilder.DropColumn(
                name: "code",
                schema: "requests",
                table: "request_types");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "code",
                schema: "requests",
                table: "request_types",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_request_types_code",
                schema: "requests",
                table: "request_types",
                column: "code",
                unique: true);
        }
    }
}
