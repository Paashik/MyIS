using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeShortName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "short_name",
                schema: "org",
                table: "employees",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE org.employees
SET short_name =
  CASE
    WHEN btrim(full_name) = '' THEN full_name
    ELSE
      split_part(btrim(full_name), ' ', 1)
      || CASE WHEN split_part(btrim(full_name), ' ', 2) <> '' THEN ' ' || left(split_part(btrim(full_name), ' ', 2), 1) || '.' ELSE '' END
      || CASE WHEN split_part(btrim(full_name), ' ', 3) <> '' THEN left(split_part(btrim(full_name), ' ', 3), 1) || '.' ELSE '' END
  END;
");

            migrationBuilder.AlterColumn<string>(
                name: "short_name",
                schema: "org",
                table: "employees",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "short_name",
                schema: "org",
                table: "employees");
        }
    }
}
