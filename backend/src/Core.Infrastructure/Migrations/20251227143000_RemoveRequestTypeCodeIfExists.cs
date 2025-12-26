using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRequestTypeCodeIfExists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE requests.request_types
  DROP CONSTRAINT IF EXISTS ck_request_types_canonical_codes;

DROP INDEX IF EXISTS requests.""IX_request_types_code"";

ALTER TABLE requests.request_types
  DROP COLUMN IF EXISTS code;
");
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
