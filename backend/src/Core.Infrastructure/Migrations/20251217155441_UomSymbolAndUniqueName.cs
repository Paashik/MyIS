using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UomSymbolAndUniqueName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Symbol",
                schema: "mdm",
                table: "unit_of_measures",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            // Data migration from legacy mapping:
            // - old: Code = Symbol, Name = "Name (796)"
            // - old (rare): Code = 796, Name = "Name [Symbol]"
            migrationBuilder.Sql(
                """
UPDATE mdm.unit_of_measures
SET "Symbol" = substring("Name" from '\\[([^\\]]+)\\]$'),
    "Name" = regexp_replace("Name", '\\s*\\[[^\\]]+\\]$', '')
WHERE "Symbol" = ''
  AND "Name" ~ '\\[[^\\]]+\\]$';

UPDATE mdm.unit_of_measures
SET "Symbol" = "Code",
    "Code" = substring("Name" from '\\((\\d{3})\\)$'),
    "Name" = regexp_replace("Name", '\\s*\\(\\d{3}\\)$', '')
WHERE "Symbol" = ''
  AND "Name" ~ '\\(\\d{3}\\)$'
  AND "Code" !~ '^\\d{3}$';

UPDATE mdm.unit_of_measures
SET "Symbol" = "Code"
WHERE "Symbol" = '';

UPDATE mdm.unit_of_measures
SET "Code" = btrim("Code"),
    "Name" = btrim("Name"),
    "Symbol" = btrim("Symbol");

WITH ranked AS (
    SELECT "Id",
           row_number() OVER (PARTITION BY "Name" ORDER BY "Code") AS rn
    FROM mdm.unit_of_measures
)
UPDATE mdm.unit_of_measures u
SET "Name" = u."Name" || ' #' || ranked.rn
FROM ranked
WHERE u."Id" = ranked."Id"
  AND ranked.rn > 1;
""");

            migrationBuilder.CreateIndex(
                name: "IX_unit_of_measures_Name",
                schema: "mdm",
                table: "unit_of_measures",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_unit_of_measures_Name",
                schema: "mdm",
                table: "unit_of_measures");

            migrationBuilder.DropColumn(
                name: "Symbol",
                schema: "mdm",
                table: "unit_of_measures");
        }
    }
}
