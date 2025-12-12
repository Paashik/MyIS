using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S2_Settings_Security_EmployeesUsersRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "org");

            migrationBuilder.AddColumn<Guid>(
                name: "employee_id",
                schema: "core",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "employees",
                schema: "org",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_employee_id",
                schema: "core",
                table: "users",
                column: "employee_id",
                unique: true,
                filter: "\"employee_id\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_users_employees_employee_id",
                schema: "core",
                table: "users",
                column: "employee_id",
                principalSchema: "org",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_employees_employee_id",
                schema: "core",
                table: "users");

            migrationBuilder.DropTable(
                name: "employees",
                schema: "org");

            migrationBuilder.DropIndex(
                name: "IX_users_employee_id",
                schema: "core",
                table: "users");

            migrationBuilder.DropColumn(
                name: "employee_id",
                schema: "core",
                table: "users");
        }
    }
}
