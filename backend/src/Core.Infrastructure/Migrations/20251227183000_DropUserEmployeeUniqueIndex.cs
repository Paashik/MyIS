using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyIS.Core.Infrastructure.Data;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20251227183000_DropUserEmployeeUniqueIndex")]
    public partial class DropUserEmployeeUniqueIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_employee_id",
                schema: "core",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "IX_users_employee_id",
                schema: "core",
                table: "users",
                column: "employee_id",
                filter: "\"employee_id\" IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_employee_id",
                schema: "core",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "IX_users_employee_id",
                schema: "core",
                table: "users",
                column: "employee_id",
                unique: true,
                filter: "\"employee_id\" IS NOT NULL");
        }
    }
}
