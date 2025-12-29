using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyIS.Core.Infrastructure.Data;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20251229121000_RenameRequestInitiatorToManager")]
    public partial class RenameRequestInitiatorToManager : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_requests_users_initiator_id",
                schema: "requests",
                table: "requests");

            migrationBuilder.RenameColumn(
                name: "initiator_id",
                schema: "requests",
                table: "requests",
                newName: "manager_id");

            migrationBuilder.RenameIndex(
                name: "IX_requests_initiator_id",
                schema: "requests",
                table: "requests",
                newName: "IX_requests_manager_id");

            migrationBuilder.AddForeignKey(
                name: "FK_requests_users_manager_id",
                schema: "requests",
                table: "requests",
                column: "manager_id",
                principalSchema: "core",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_requests_users_manager_id",
                schema: "requests",
                table: "requests");

            migrationBuilder.RenameIndex(
                name: "IX_requests_manager_id",
                schema: "requests",
                table: "requests",
                newName: "IX_requests_initiator_id");

            migrationBuilder.RenameColumn(
                name: "manager_id",
                schema: "requests",
                table: "requests",
                newName: "initiator_id");

            migrationBuilder.AddForeignKey(
                name: "FK_requests_users_initiator_id",
                schema: "requests",
                table: "requests",
                column: "initiator_id",
                principalSchema: "core",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
